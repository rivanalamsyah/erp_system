# TECHNICAL AUDIT — Arsitektur, Kualitas Kode, Domain Logic & Blazor Frontend

> **Tanggal Audit:** 12 November 2026  
> **Fokus Evaluasi:** Clean Architecture, Domain-Driven Design (DDD), SOLID Principles, State Management Blazor Server, Code Duplication, dan Technical Debt

---

## 1. Evaluasi Arsitektur & Separation of Concerns

### 1.1 Kekuatan Arsitektur yang Sudah Terbukti
1. **Arah Dependensi Proyek Bersih (Verified by `NetArchTest`):**
   - `ERP.Domain` tidak memiliki referensi ke `ERP.Application`, `ERP.Infrastructure`, maupun `ERP.Web` (`tests/ERP.Architecture.Tests/CleanArchitectureTests.cs:14-48`).
   - `ERP.Application` tidak memiliki referensi ke `ERP.Infrastructure` maupun `ERP.Web`.
2. **Rich Domain Model (Bukan Anemic Domain Model):**
   - Seluruh 34 entitas domain menggunakan `private set` pada properti, konstruktor `private` untuk EF Core, static factory method `Create(...)`, serta metode mutasi domain yang mengenkapsulasi *invariant* bisnis (contoh: `StockBalance.AddStock`, `StockBalance.ReserveStock`, `JournalEntry.Post`, `JournalEntry.CreateReversal`, `SalesInvoice.ApplyPayment`, `PayrollPeriod.CalculateItem`).

### 1.2 Temuan Arsitektur & Kualitas Kode

---

#### [ARCH-CRIT-01] Konkurensi `Scoped ApplicationDbContext` pada Blazor Interactive Server Circuit & Pelanggaran Clean Architecture di 25+ Halaman `.razor`
- **Severity:** `Critical`
- **Lokasi File & Baris:**
  - `src/ERP.Infrastructure/DependencyInjection.cs:26-35`
  - `src/ERP.Web/Components/Layout/MainLayout.razor:6, 179-189`
  - `src/ERP.Web/Components/Pages/Home.razor:3, 260-335`
  - `src/ERP.Web/Components/Pages/Master/DepartmentsPage.razor:5, 118-174`
  - `src/ERP.Web/Components/Pages/Security/UsersPage.razor:5`, `RolesPage.razor:4`, `AuditLogsPage.razor:3`
  - Serta 20+ halaman `.razor` lainnya yang mendeklarasikan `@inject IApplicationDbContext DbContext`.
- **Akar Masalah (Root Cause):**
  1. Pada aplikasi **Blazor Interactive Server**, satu *Dependency Injection Scope* hidup sepanjang umur koneksi WebSocket/SignalR (*Circuit*) pengguna, bukan per HTTP request.
  2. `ApplicationDbContext` didaftarkan sebagai `Scoped` (`AddDbContext<ApplicationDbContext>`).
  3. `MainLayout.razor` menginjeksikan `IApplicationDbContext DbContext` dan mengeksekusi query `await DbContext.Branches.AsNoTracking().ToListAsync()` pada `OnInitializedAsync()` (`MainLayout.razor:181`), sementara secara bersamaan komponen halaman anak (`Body`) juga menginjeksikan instans `IApplicationDbContext` yang **sama** dan menjalankan query pada `OnInitializedAsync()` atau `ServerReload()`.
  4. Selain itu, halaman seperti `Home.razor`, `DepartmentsPage.razor`, `UsersPage.razor`, `RolesPage.razor`, `AuditLogsPage.razor`, `ProductsPage.razor` (baris 250–266), dan `LeavesPage.razor` (baris 135–145) melakukan query bahkan `DbContext.SaveChangesAsync()` secara langsung dari komponen UI tanpa melewati layer `ERP.Application` (MediatR).
- **Dampak Sistem:**
  - EF Core `DbContext` **tidak thread-safe** dan tidak mendukung operasi asinkron konkuren pada satu instans yang sama. Ketika komponen induk (`MainLayout`), komponen anak, atau event UI (seperti *debounce search* dan klik tab) berjalan bersamaan dalam satu circuit, sistem rentan melempar `InvalidOperationException: A second operation was started on this context instance before a previous operation completed`.
  - Karena `DbContext` hidup selama sesi tab browser terbuka, seluruh entitas yang dimuat tanpa `.AsNoTracking()` tetap tertahan di `ChangeTracker` memori server, menyebabkan kebocoran memori (*memory bloat*) dan potensi konflik status entitas (*stale state*).
- **Rekomendasi Perbaikan Teknis:**
  - Gunakan `IDbContextFactory<ApplicationDbContext>` untuk membuat unit kerja (`DbContext`) berumur pendek per operasi/handler, atau pastikan seluruh halaman Blazor hanya berkomunikasi melalui `ISender` (MediatR) di mana setiap request atau factory mengelola masa hidup `DbContext` secara terisolasi, serta hapus seluruh `@inject IApplicationDbContext` dan `@inject ApplicationDbContext` dari file `.razor`.

---

#### [ARCH-CRIT-02] Pemanggilan `SaveChangesAsync` Prematur Non-Transaksional di `DocumentSequenceService`
- **Severity:** `Critical`
- **Lokasi File & Baris:**
  - `src/ERP.Infrastructure/Services/DocumentSequenceService.cs:14-47`
  - `src/ERP.Application/Features/Procurement/GoodsReceipts/CreateGoodsReceiptCommand.cs:67-95`
  - `src/ERP.Application/Features/Sales/Deliveries/CreateDeliveryCommand.cs:66-95`
  - `src/ERP.Application/Features/Finance/Payments/CreatePaymentCommand.cs:45-77`
- **Akar Masalah (Root Cause):**
  Di dalam `DocumentSequenceService.NextDocumentNumberAsync` (baris 44), metode ini langsung memanggil `await context.SaveChangesAsync(cancellationToken)` untuk menyimpan kenaikan nomor urut dokumen, **sebelum** Command Handler pemanggil menyelesaikan validasi bisnis item dan menyimpan entitas transaksi utama.
  Contoh nyata pada `CreateGoodsReceiptCommand.cs`:
  - Baris 67: `var grnNumber = await sequenceService.NextDocumentNumberAsync(...)` -> **langsung commit ke database!**
  - Baris 76–91: Melakukan iterasi item dan memvalidasi apakah `poItem.ReceivedQuantity + itemReq.QuantityReceived > poItem.Quantity`. Jika validasi gagal dan mengembalikan `Result.Failure("Total kuantitas diterima melebihi kuantitas PO...")`, nomor GRN sudah terlanjur terpakai dan tersimpan di database!
  - Lebih buruk lagi, jika ada entitas lain yang sudah dimodifikasi di `ChangeTracker` sebelum pemanggilan `NextDocumentNumberAsync`, perubahan tersebut ikut ter-commit secara parsial meskipun handler gagal di tengah jalan.
- **Dampak Sistem:**
  - *Sequence number jumping/gaps* (nomor dokumen loncat setiap kali validasi item gagal).
  - Risiko *partial commit* (data korup/setengah tersimpan) karena dua kali pemanggilan `SaveChangesAsync()` tanpa pembungkus transaksi database eksplisit (`BeginTransactionAsync`).
  - *Race condition* saat dua pengguna membuat dokumen secara bersamaan karena tidak adanya kunci baris (`SELECT ... FOR UPDATE` atau `xmin` concurrency token) pada tabel `master.document_sequences`.
- **Rekomendasi Perbaikan Teknis:**
  - Hapus `await context.SaveChangesAsync()` dari dalam `DocumentSequenceService.NextDocumentNumberAsync` sehingga perubahan `DocumentSequence` di-commit secara atomik dalam satu transaksi bersama entitas dokumen utama pada akhir Command Handler, atau gunakan operasi SQL atomik PostgreSQL (`INSERT ... ON CONFLICT DO UPDATE ... RETURNING last_number`) di dalam transaksi.

---

#### [ARCH-HIGH-01] Konteks Multi-Perusahaan & Multi-Cabang (`Branch Switcher`) Tidak Berfungsi dan Hardcoded ke `FirstOrDefaultAsync()` di 16 Halaman UI
- **Severity:** `High`
- **Lokasi File & Baris:**
  - `src/ERP.Web/Components/Layout/MainLayout.razor:33-45, 169, 194-199`
  - `ProductsPage.razor:328`, `CustomersPage.razor:273`, `SuppliersPage.razor:292`, `StockTransfersPage.razor:259-260`, `StockAdjustmentsPage.razor:238-239`, `PurchaseRequestsPage.razor:328-329`, `PurchaseOrdersPage.razor:375-376`, `GoodsReceiptsPage.razor:369-370`, `VendorBillsPage.razor:254-255`, `QuotationsPage.razor:371-372`, `SalesOrdersPage.razor:443-444`, `DeliveriesPage.razor:376-377`, `SalesInvoicesPage.razor:261-262`, `JournalEntriesPage.razor:362-363`, `PaymentsPage.razor:410-411`, `EmployeesPage.razor:169`.
- **Akar Masalah (Root Cause):**
  1. Pada `MainLayout.razor:194-199`, metode `SwitchBranch(Guid branchId, string branchName)` hanya mengubah variabel lokal `_activeBranchId` dan `_activeBranchName` di dalam `MainLayout` lalu menampilkan `Snackbar`, tanpa meneruskan nilai tersebut melalui `<CascadingValue>` ataupun memperbarui state di `ICurrentUserService`.
  2. Di 16 halaman pembuatan master dan transaksi, kode UI mengambil perusahaan dan cabang dengan cara:
     ```csharp
     var company = await DbContext.Companies.FirstOrDefaultAsync();
     var branch = await DbContext.Branches.FirstOrDefaultAsync();
     ```
- **Dampak Sistem:**
  - Ketika sistem memiliki lebih dari satu Perusahaan (`Company`) atau lebih dari satu Cabang (`Branch`), seluruh transaksi baru (PO, SO, GRN, Delivery, Invoice, Jurnal, Payment, Karyawan) selalu tercatat pada perusahaan/cabang pertama di tabel database, terlepas dari cabang mana yang sedang dipilih pengguna di *Top AppBar*.
- **Rekomendasi Perbaikan Teknis:**
  - Buat layanan state sesi (`IUserContextState` / `<CascadingValue>`) yang diinisialisasi dari klaim `CompanyId` dan `BranchId` pengguna saat login, terhubung dengan *Branch Switcher* di `MainLayout.razor` (divalidasi terhadap `AllowedBranchIds`), dan gunakan konteks tersebut secara konsisten di seluruh Command/Query.

---

#### [ARCH-HIGH-02] Ketiadaan Integrasi Otomatis Sub-Ledger ke General Ledger (GL) & Penegakan Periode Fiskal Tutup (`BR-ACC-002`)
- **Severity:** `High`
- **Lokasi File & Baris:**
  - `src/ERP.Application/Features/Procurement/GoodsReceipts/CreateGoodsReceiptCommand.cs:110-171` (`ReceiveGoodsReceiptCommandHandler`)
  - `src/ERP.Application/Features/Procurement/VendorBills/CreateVendorBillCommand.cs:80-102` (`PostVendorBillCommandHandler`)
  - `src/ERP.Application/Features/Sales/Deliveries/CreateDeliveryCommand.cs:110-179` (`DispatchDeliveryCommandHandler`)
  - `src/ERP.Application/Features/Sales/SalesInvoices/CreateSalesInvoiceCommand.cs:82-105` (`PostSalesInvoiceCommandHandler`)
  - `src/ERP.Application/Features/Finance/Payments/CreatePaymentCommand.cs:90-156` (`PostPaymentCommandHandler`)
  - `src/ERP.Application/Features/Hr/Payroll/PayPayrollCommand.cs:11-40` (`PayPayrollCommandHandler`)
  - `src/ERP.Application/Features/Finance/JournalEntries/CreateJournalEntryCommand.cs:98-119` (`PostJournalEntryCommandHandler`)
- **Akar Masalah (Root Cause):**
  1. Handler transaksi operasional (GRN, Vendor Bill, Delivery, Sales Invoice, Payment, dan Payroll Disbursement) hanya mengubah status dokumen sub-ledger dan saldo stok/kas, tetapi belum memanggil layanan akuntansi untuk menghasilkan `JournalEntry` otomatis ke tabel `finance.journal_entries`.
  2. Meskipun entitas `FiscalPeriod` dan fitur *Tutup Buku* (`CloseFiscalPeriodCommand`) telah dibuat, tidak ada satu pun handler posting (`PostJournalEntryCommand`, `PostPaymentCommand`, `PostSalesInvoiceCommand`, `PostVendorBillCommand`) yang memeriksa apakah tanggal transaksi berada pada `FiscalPeriod` yang memiliki `IsClosed == true` (`BR-ACC-002`).
- **Dampak Sistem:**
  - Laporan Keuangan (`Balance Sheet`, `Profit & Loss`, `Trial Balance`) yang dihitung dari `finance.journal_lines` tidak mencerminkan transaksi pembelian, penjualan, persediaan, pembayaran kas/bank, maupun penggajian kecuali pengguna membuat jurnal manual satu per satu.
  - Transaksi masih dapat diposting secara mundur (*backdated*) ke periode bulan/tahun buku yang sudah ditutup (`IsClosed = true`).
- **Rekomendasi Perbaikan Teknis:**
  - Implementasikan `IAccountingPostingService` di layer Application/Infrastructure untuk mengotomasi pembuatan jurnal GL saat dokumen sub-ledger diposting, serta tambahkan validasi periode fiskal aktif (`!FiscalPeriod.IsClosed`) sebelum mengizinkan posting jurnal atau dokumen keuangan.

---

#### [ARCH-MED-01] Ketidakkonsistenan Kontrak Return MediatR (`PagedResult<T>` vs `Result<PagedResult<T>>`) dan Duplikasi Properti `Error`
- **Severity:** `Medium`
- **Lokasi File & Baris:**
  - `src/ERP.Application/Features/Procurement/GoodsReceipts/GetGoodsReceiptsQuery.cs:15` (`IRequest<PagedResult<GoodsReceiptDto>>`)
  - `src/ERP.Application/Features/Procurement/VendorBills/GetVendorBillsQuery.cs:15` (`IRequest<PagedResult<VendorBillDto>>`)
  - `src/ERP.Application/Common/Behaviors/AuthorizationPipelineBehavior.cs:23, 33, 55-68`
  - `src/ERP.Shared/Common/Result.cs:9-15`
- **Akar Masalah (Root Cause):**
  - Sebanyak 65 dari 67 request MediatR mengembalikan `Result` atau `Result<T>`, tetapi `GetGoodsReceiptsQuery` dan `GetVendorBillsQuery` mengembalikan `PagedResult<T>` secara langsung tanpa dibungkus `Result<T>`.
  - Di dalam `AuthorizationPipelineBehavior.CreateFailureResult<TResponse>` (baris 67), apabila `TResponse` bukan `Result` atau `Result<T>`, metode tersebut melempar `throw new UnauthorizedAccessException(error.Description)` alih-alih mengembalikan objek kegagalan yang dapat ditangani dengan aman oleh UI!
  - Selain itu, record `Error` (`Result.cs:9-15`) memiliki properti `Description` sekaligus properti alias `public string Message => Description;` karena sebagian kode memanggil `.Error.Description` dan sebagian lagi memanggil `.Error.Message`.
- **Dampak Sistem:**
  - Penolakan izin pada `GetGoodsReceiptsQuery` dan `GetVendorBillsQuery` menyebabkan *unhandled exception* yang mematikan koneksi SignalR circuit pengguna.
- **Rekomendasi Perbaikan Teknis:**
  - Standarisasi seluruh Query/Command MediatR agar mengembalikan `Result<T>`, termasuk mengubah `GetGoodsReceiptsQuery` dan `GetVendorBillsQuery` menjadi `IRequest<Result<PagedResult<...>>>`.

---

#### [ARCH-MED-02] Halaman Pengaturan Sistem (`SettingsPage.razor`) Hanya Berupa Mock UI Tanpa Persistensi
- **Severity:** `Medium`
- **Lokasi File & Baris:**
  - `src/ERP.Web/Components/Pages/Settings/SettingsPage.razor:165-209`
- **Akar Masalah (Root Cause):**
  - Field konfigurasi pada `SettingsPage.razor` (`_ppnRate`, `_currencyCode`, `_costingMethod`, `_requirePoApproval`, `_minPoApprovalAmount`, `_strictCreditLimit`, dll.) di-hardcode di memori komponen, dan metode `SaveSettings()`, `TriggerBackup()`, serta `ClearCache()` hanya melakukan `await Task.Delay(...)` lalu menampilkan `Snackbar`.
- **Dampak Sistem:**
  - Perubahan konfigurasi oleh Administrator hilang begitu halaman dimuat ulang, dan aturan bisnis seperti tarif PPN 11% masih di-hardcode di dalam entitas/handler (`PurchaseOrder.cs:109`, `Quotation.cs:108`, `SalesOrder.cs:111`, `SalesInvoice.cs:46`).
- **Rekomendasi Perbaikan Teknis:**
  - Sediakan entitas/tabel `SystemSetting` (atau simpan pada konfigurasi `Company`) dan hubungkan `SettingsPage.razor` ke Command/Query MediatR yang sesungguhnya.

---

#### [ARCH-MED-03] Ketiadaan Global Error Boundary pada Layout Blazor Interactive Server
- **Severity:** `Medium`
- **Lokasi File & Baris:**
  - `src/ERP.Web/Components/Layout/MainLayout.razor:141-143`
- **Akar Masalah (Root Cause):**
  - `<MudMainContent>` merender `@Body` secara langsung tanpa pembungkus `<ErrorBoundary>`.
- **Dampak Sistem:**
  - Apabila satu komponen halaman mengalami *unhandled exception*, seluruh circuit Blazor pengguna terputus dan hanya menampilkan banner kuning standar browser (`An unhandled error has occurred. Reload`), memaksa pengguna memuat ulang seluruh aplikasi.
- **Rekomendasi Perbaikan Teknis:**
  - Bungkus `@Body` di `MainLayout.razor` dengan komponen `<ErrorBoundary>` yang menampilkan UI pemulihan ramah pengguna (MudAlert + tombol *Coba Lagi*) serta mencatat error ke logger.

