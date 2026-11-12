# DATABASE AUDIT — Evaluasi Skema PostgreSQL, Entity Framework Core 10, Relasi, Indeks & Konkurensi

> **Tanggal Audit:** 12 November 2026  
> **Database Engine & Provider:** PostgreSQL 17 (`Npgsql.EntityFrameworkCore.PostgreSQL` v10.0.1)  
> **Sumber Kebenaran Konfigurasi:** `src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs` (723 baris) & Migrasi `20260329172235_InitialEnterpriseSchema.cs`

---

## 1. Arsitektur Skema & Pemetaan Tabel (8 Skema PostgreSQL)

Database dirancang menggunakan pendekatan **Multi-Schema Logical Partitioning** di dalam satu database PostgreSQL (`enterprise_erp`), dengan total **42 tabel** (34 tabel bisnis, 7 tabel ASP.NET Core Identity + PBAC, dan 1 tabel Audit Trail):

| Skema PostgreSQL | Jumlah Tabel | Daftar Tabel Aktual |
| :--- | :---: | :--- |
| **`identity`** | 9 | `users`, `roles`, `user_roles`, `user_claims`, `user_logins`, `user_tokens`, `role_claims`, `role_permissions`, `user_branches` |
| **`master`** | 9 | `companies`, `branches`, `departments`, `units_of_measure`, `product_categories`, `products`, `customers`, `suppliers`, `document_sequences` |
| **`inventory`** | 7 | `warehouses`, `stock_balances`, `stock_movements`, `stock_transfers`, `stock_transfer_items`, `stock_adjustments`, `stock_adjustment_items` |
| **`procurement`** | 7 | `purchase_requests`, `purchase_request_items`, `purchase_orders`, `purchase_order_items`, `goods_receipts`, `goods_receipt_items`, `vendor_bills` |
| **`sales`** | 7 | `quotations`, `quotation_items`, `sales_orders`, `sales_order_items`, `deliveries`, `delivery_items`, `sales_invoices` |
| **`finance`** | 7 | `chart_of_accounts`, `fiscal_periods`, `journal_entries`, `journal_lines`, `cash_bank_accounts`, `payments`, `payment_allocations` |
| **`hr`** | 6 | `employees`, `attendance_records`, `leave_types`, `leave_requests`, `payroll_periods`, `payroll_items` |
| **`audit`** | 1 | `audit_logs` |

---

## 2. Praktik Terbaik Database yang Sudah Diterapkan

1. **Presisi Numerik Finansial & Kuantitas:**
   - Seluruh kolom mata uang (`SubtotalAmount`, `TaxAmount`, `TotalAmount`, `Debit`, `Credit`, `BasicSalary`, `CreditLimit`) menggunakan `numeric(18,2)`.
   - Seluruh kolom kuantitas dan harga rata-rata (`PhysicalQuantity`, `ReservedQuantity`, `AverageCost`, `Quantity`, `UnitCost`) menggunakan `numeric(18,4)`.
2. **Optimistic Concurrency via PostgreSQL System Column (`xmin`):**
   - Sebanyak 22 entitas utama memetakan properti `uint Version` ke kolom sistem PostgreSQL `xmin` (`b.Property(x => x.Version).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()`).
3. **Referential Integrity (`DeleteBehavior.Restrict` & `Cascade`):**
   - Relasi ke Master Data (`Company`, `Branch`, `Product`, `Customer`, `Supplier`, `Warehouse`, `ChartOfAccount`) menggunakan `DeleteBehavior.Restrict` untuk mencegah penghapusan master yang sudah memiliki histori transaksi.
   - Relasi *Aggregate Root* ke *Child Line Items* (`PurchaseOrderItem`, `SalesOrderItem`, `JournalLine`, `PayrollItem`, dll.) menggunakan `DeleteBehavior.Cascade`.
4. **JSONB Audit Snapshots:**
   - Tabel `audit.audit_logs` menyimpan `old_values`, `new_values`, dan `affected_columns` dengan tipe `jsonb` asli PostgreSQL (`ApplicationDbContext.cs:717-719`).

---

## 3. Temuan Audit Database & Entity Framework Core

---

### [DB-CRIT-01] Ketiadaan Optimistic Concurrency Token (`xmin`) pada `StockBalance` dan `DocumentSequence`
- **Severity:** `Critical`
- **Lokasi File & Baris:**
  - `src/ERP.Domain/Entities/Inventory/StockBalance.cs:5-14`
  - `src/ERP.Domain/Entities/Master/DocumentSequence.cs:5-13`
  - `src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs:261-274` (`DocumentSequence`)
  - `src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs:288-300` (`StockBalance`)
- **Akar Masalah (Root Cause):**
  - Berbeda dengan `Product` atau `SalesOrder` yang mewarisi `BaseEntity<Guid>` (yang memiliki `public uint Version { get; private set; }`), class `StockBalance` dan `DocumentSequence` **tidak mewarisi `BaseEntity<Guid>`** dan tidak memiliki properti `uint Version`.
  - Di `ApplicationDbContext.cs:261-274` dan `288-300`, kedua tabel tersebut adalah tabel dengan frekuensi mutasi konkuren tertinggi di seluruh ERP (setiap konfirmasi SO, penerimaan GRN, pengiriman Delivery, transfer gudang, dan stok opname memodifikasi `StockBalance`; setiap pembuatan dokumen memodifikasi `DocumentSequence`), namun **sama sekali tidak memiliki konfigurasi `.IsConcurrencyToken()` (`xmin`)**!
- **Dampak Sistem:**
  - **Lost Update Anomaly pada Saldo Stok & HPP:** Jika dua staf gudang/sales memproses pengiriman atau penerimaan barang untuk produk dan gudang yang sama pada waktu berdekatan, transaksi kedua akan menimpa (`OVERWRITE`) `PhysicalQuantity`, `ReservedQuantity`, dan `AverageCost` transaksi pertama tanpa mendeteksi `DbUpdateConcurrencyException`. Saldo stok fisik di database menjadi tidak sinkron dengan histori kartu stok (`inventory.stock_movements`).
  - **Duplikasi Nomor Dokumen:** Dua request pembuatan PO/SO/GRN paralel akan membaca `LastNumber` yang sama pada `DocumentSequence` dan menghasilkan nomor dokumen kembar (yang kemudian gagal dengan *unique index violation* tanpa mekanisme *retry*).
- **Rekomendasi Perbaikan Teknis:**
  - Tambahkan properti `public uint Version { get; private set; }` pada `StockBalance` dan `DocumentSequence`, lalu konfigurasikan `.HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()` di `ApplicationDbContext.cs` (tidak memerlukan migrasi DDL destruktif karena `xmin` adalah kolom sistem bawaan pada setiap tabel PostgreSQL), serta tambahkan penanganan *retry* pada `DbUpdateConcurrencyException`.

---

### [DB-HIGH-01] Ketiadaan Global Query Filter Multi-Tenant (`HasQueryFilter`) pada `FiscalPeriod`, `AttendanceRecord`, `LeaveRequest`, dan `AuditLog`
- **Severity:** `High`
- **Lokasi File & Baris:**
  - `src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs:577-583` (`FiscalPeriod`)
  - `src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs:662-669` (`AttendanceRecord`)
  - `src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs:680-687` (`LeaveRequest`)
  - `src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs:714-720` (`AuditLog`)
- **Akar Masalah (Root Cause):**
  1. `FiscalPeriod` dan `AuditLog` memiliki kolom `CompanyId`, namun konfigurasi `ApplicationDbContext` tidak menyertakan `b.HasQueryFilter(...)` pada keduanya. Bahkan `GetFiscalPeriodsQuery.cs:19-33` mengambil `context.FiscalPeriods.AsNoTracking()` tanpa filter `CompanyId` sama sekali!
  2. `AttendanceRecord` dan `LeaveRequest` tidak menyimpan `CompanyId` dan tidak memiliki `HasQueryFilter` melalui navigasi `Employee`. Di `GetAttendanceRecordsQuery.cs:26-30` dan `GetLeaveRequestsQuery.cs:24-28`, karena `Employee` dikenai `HasQueryFilter` sementara `AttendanceRecord` dan `LeaveRequest` tidak, EF Core dapat menghasilkan *LEFT JOIN* di mana baris presensi/cuti dari tenant lain tetap ikut tertarik dengan `Employee == null` (atau memicu peringatan EF Core 10622: *Entity type 'Employee' has a global query filter defined and is the required end of a relationship with 'AttendanceRecord'*).
- **Dampak Sistem:**
  - Kebocoran data lintas perusahaan (*cross-tenant data leakage*) pada periode fiskal, presensi, pengajuan cuti, dan log audit.
- **Rekomendasi Perbaikan Teknis:**
  - Tambahkan `HasQueryFilter` pada `FiscalPeriod`, `AttendanceRecord` (melalui `!x.Employee.IsDeleted && ...`), `LeaveRequest`, dan `AuditLog`, serta tambahkan filter `CompanyId` pada `GetFiscalPeriodsQuery` dan `AuditLogsPage.razor`.

---

### [DB-HIGH-02] Indeks Komposit yang Hilang pada Kolom Filter & Pengurutan Utama
- **Severity:** `High`
- **Lokasi File & Baris:**
  - `src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs:302-322` (`StockMovement`)
  - `src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs:585-610` (`JournalEntry` & `JournalLine`)
  - `src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs:714-720` (`AuditLog`)
- **Akar Masalah (Root Cause):**
  1. **Tabel `audit.audit_logs`:** `AuditLogsPage.razor:141-143` menjalankan `OrderByDescending(a => a.CreatedAtUtc).Take(200)` dengan filter `ActionType` dan `TableName`, tetapi `ApplicationDbContext.cs:714-720` **tidak mendefinisikan satu pun indeks** di luar Primary Key `Id`. Seiring pertumbuhan log audit, query ini akan melakukan *Full Sequential Table Scan* + *Sort*.
  2. **Tabel `finance.journal_entries`:** Query laporan keuangan (`GetFinancialReportsQuery.cs:33, 95, 153`) memfilter berdasarkan `(CompanyId, Status, JournalDate)`, tetapi tabel `journal_entries` hanya memiliki indeks unik pada `(CompanyId, JournalNumber)`.
  3. **Tabel `inventory.stock_movements`:** Indeks saat ini adalah `(WarehouseId, ProductId, MovementDateUtc)` (`ApplicationDbContext.cs:316`), namun query filter di `Home.razor` atau laporan mutasi per perusahaan sering memfilter berdasarkan `(CompanyId, MovementDateUtc)`.
- **Rekomendasi Perbaikan Teknis:**
  - Tambahkan indeks komposit:
    - `audit.audit_logs`: `IX_audit_logs_CompanyId_CreatedAtUtc` dan `IX_audit_logs_TableName_ActionType`
    - `finance.journal_entries`: `IX_journal_entries_CompanyId_Status_JournalDate`
    - `sales.sales_invoices`: `IX_sales_invoices_CompanyId_Status_InvoiceDate`
    - `procurement.vendor_bills`: `IX_vendor_bills_CompanyId_Status_BillDate`

---

### [DB-MED-01] Penggunaan `.ToLower().Contains(...)` pada Seluruh Query Pencarian Mengabaikan Indeks B-Tree PostgreSQL
- **Severity:** `Medium`
- **Lokasi File & Baris:**
  - Ditemukan di 18 Query Handler (misal `GetProductsQuery.cs:51-53`, `GetCustomersQuery.cs:35-37`, `GetSalesOrdersQuery.cs:40-41`, `GetJournalEntriesQuery.cs:31-33`, dll.).
- **Akar Masalah & Dampak:**
  - Ekspresi `p.Name.ToLower().Contains(search)` diterjemahkan menjadi `LOWER(p."Name") LIKE '%...%'` di SQL. Untuk pencarian awalan atau pencocokan case-insensitive di PostgreSQL, penggunaan `EF.Functions.ILike(p.Name, $"%{search}%")` lebih idiomatis pada Npgsql dan dapat didukung oleh indeks *GIN trigram* (`pg_trgm`) apabila volume data mencapai ratusan ribu baris.

---

### [DB-MED-02] Konfigurasi Migration & Seeder Tidak Dieksekusi pada Environment `Production`
- **Severity:** `Medium`
- **Lokasi File & Baris:**
  - `src/ERP.Web/Program.cs:64-75`
  - `src/ERP.Infrastructure/Persistence/Seeders/DatabaseSeeder.cs:29`
- **Akar Masalah & Dampak:**
  - `DatabaseSeeder.SeedAsync(app.Services)` (yang di dalamnya memanggil `await context.Database.MigrateAsync()`) hanya dipanggil di dalam blok `if (app.Environment.IsDevelopment())` (`Program.cs:64`).
  - Saat aplikasi dijalankan melalui `docker-compose up` (di mana `Dockerfile:35` dan `docker-compose.yml:38` menyetel `ASPNETCORE_ENVIRONMENT=Production`), migrasi skema database **tidak pernah dijalankan**, sehingga container aplikasi gagal saat pertama kali mengakses tabel `identity.users` atau `master.companies` yang belum ada.

