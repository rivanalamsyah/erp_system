# FEATURE MAPPING — Pemetaan End-to-End Modul, Fitur & Alur Bisnis

> **Tanggal Audit:** 12 November 2026  
> **Cakupan:** Seluruh Modul Bisnis, 26 Halaman Blazor UI, 2 Endpoint Minimal API, 67 MediatR Requests (Commands & Queries), dan 34 Entitas Domain

---

## 1. Peta Modul Bisnis & Status Implementasi Aktual

| No | Modul Bisnis | Skema DB | Entitas Domain | Halaman UI (`.razor`) | Status Fungsional Aktual |
| :-: | :--- | :--- | :--- | :--- | :--- |
| **1** | **Authentication & Security (PBAC)** | `identity`, `audit` | `ApplicationUser`, `ApplicationRole`, `RolePermission`, `UserBranch`, `AuditLog` | `Login.razor`, `ProfilePage.razor`, `ChangePasswordPage.razor`, `UsersPage.razor`, `RolesPage.razor`, `AuditLogsPage.razor` | ⚠️ **Bermasalah Kritis:** Login hanya berfungsi penuh untuk `SUPER_ADMIN` (`SEC-CRIT-01`). Halaman Users, Roles, dan AuditLogs tidak diproteksi `@attribute [Authorize]` (`SEC-CRIT-03`). |
| **2** | **Master Data Management** | `master` | `Company`, `Branch`, `Department`, `UnitOfMeasure`, `ProductCategory`, `Product`, `Customer`, `Supplier`, `DocumentSequence` | `CompaniesPage.razor`, `BranchesPage.razor`, `DepartmentsPage.razor`, `ProductsPage.razor`, `CustomersPage.razor`, `SuppliersPage.razor` | 🟡 **Berfungsi (Dengan Celah):** CRUD berjalan, namun halaman tidak memiliki `@attribute [Authorize]` dan pembuatan data selalu memilih `Companies.FirstOrDefaultAsync()`. |
| **3** | **Inventory & Warehouse** | `inventory` | `Warehouse`, `StockBalance`, `StockMovement`, `StockTransfer`, `StockTransferItem`, `StockAdjustment`, `StockAdjustmentItem` | `StockBalancesPage.razor`, `StockTransfersPage.razor`, `StockAdjustmentsPage.razor` | 🟡 **Berfungsi (Dengan Celah):** Kartu stok, mutasi antar gudang, dan stok opname berjalan; namun `StockBalance` belum memiliki token konkurensi `xmin` (`DB-CRIT-01`) dan terdapat query N+1 (`PERF-HIGH-01`). |
| **4** | **Procurement (Procure-to-Pay / P2P)** | `procurement` | `PurchaseRequest`, `PurchaseRequestItem`, `PurchaseOrder`, `PurchaseOrderItem`, `GoodsReceipt`, `GoodsReceiptItem`, `VendorBill` | `PurchaseRequestsPage.razor`, `PurchaseOrdersPage.razor`, `GoodsReceiptsPage.razor`, `VendorBillsPage.razor` | 🟡 **Berfungsi Sebagian:** Alur PR -> PO -> GRN -> Vendor Bill berjalan dan menambah stok fisik serta Moving Average Cost, namun **belum menghasilkan jurnal GL otomatis** (`ARCH-HIGH-02`). |
| **5** | **Sales (Order-to-Cash / O2C)** | `sales` | `Quotation`, `QuotationItem`, `SalesOrder`, `SalesOrderItem`, `Delivery`, `DeliveryItem`, `SalesInvoice` | `QuotationsPage.razor`, `SalesOrdersPage.razor`, `DeliveriesPage.razor`, `SalesInvoicesPage.razor` | 🟡 **Berfungsi Sebagian:** Credit Limit Check (`BR-SAL-001`), reservasi stok saat SO Confirm, pengurangan stok saat Delivery, dan penerbitan Invoice berjalan, namun **belum menghasilkan jurnal HPP/Piutang otomatis** (`ARCH-HIGH-02`). |
| **6** | **Finance & Accounting (Record-to-Report / R2R)** | `finance` | `ChartOfAccount`, `FiscalPeriod`, `JournalEntry`, `JournalLine`, `CashBankAccount`, `Payment`, `PaymentAllocation` | `ChartOfAccountsPage.razor`, `JournalEntriesPage.razor`, `PaymentsPage.razor`, `PeriodClosingPage.razor` | 🟡 **Berfungsi Sebagian:** Jurnal manual seimbang (`BR-ACC-001`), pelunasan AR/AP (`Payment`), dan penutupan periode berjalan, namun pengecekan periode tutup (`BR-ACC-002`) belum diterapkan saat posting jurnal/pembayaran. |
| **7** | **Human Resource & Payroll (H2R)** | `hr` | `Employee`, `AttendanceRecord`, `LeaveType`, `LeaveRequest`, `PayrollPeriod`, `PayrollItem` | `EmployeesPage.razor`, `AttendancePage.razor`, `LeavesPage.razor`, `PayrollPage.razor` | 🔴 **Crash di UI (`SEC-CRIT-02`):** Seluruh Command/Query HR sudah diimplementasikan, tetapi 4 halaman UI menggunakan `[Authorize(Policy = ...)]` yang belum terdaftar di `Program.cs` sehingga memicu `InvalidOperationException`. |
| **8** | **Reports & Executive Analytics** | Lintas Skema | *(Read Models / DTOs)* | `Home.razor`, `FinancialReportPage.razor`, `InventoryReportPage.razor`, `SalesReportPage.razor` | 🔴 **Crash di UI (`SEC-CRIT-02`) & In-Memory Bottleneck (`PERF-CRIT-01`):** 3 halaman laporan gagal dibuka karena Policy tidak terdaftar, dan query laporan menarik seluruh isi tabel ke memori C#. |
| **9** | **System Settings & Maintenance** | - | *(Tidak ada entitas)* | `SettingsPage.razor` | 🔴 **Crash di UI (`SEC-CRIT-02`) & UI Mock (`ARCH-MED-02`):** Rute `/settings` crash karena Policy tidak terdaftar, dan tombol simpan hanya menampilkan notifikasi Snackbar tanpa persistensi database. |

---

## 2. Pemetaan Lengkap Rute Frontend (`.razor`), Otorisasi & Ketergantungan Backend

Tabel berikut memetakan seluruh **26 halaman Blazor** di `src/ERP.Web/Components/Pages/` beserta atribut otorisasi aktual, injeksi `DbContext`, dan pemicu MediatR:

| No | Route URL | File Komponen (`.razor`) | Atribut `[Authorize]` Aktual | Injeksi `DbContext` Langsung? | Request MediatR yang Dipanggil | Status Aktual |
| :-: | :--- | :--- | :--- | :---: | :--- | :--- |
| 1 | `/` | `Home.razor` | ❌ **Tidak Ada** | ⚠️ Ya (`IApplicationDbContext`) | *(Tidak ada — query langsung ke 11 DbSet di `Home.razor:262-331`)* | Rentan akses anonim (`SEC-CRIT-03`) & konkurensi `DbContext` (`ARCH-CRIT-01`) |
| 2 | `/login` | `Auth/Login.razor` | `@attribute [AllowAnonymous]` | Tidak | Form POST ke `/api/auth/login` | Menampilkan kredensial SuperAdmin di UI publik (`SEC-MED-02`) |
| 3 | `/profile` | `Auth/ProfilePage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`ApplicationDbContext`) | *(Query langsung `UserManager`, `RolePermissions`, `UserBranches`)* | Berfungsi |
| 4 | `/change-password` | `Auth/ChangePasswordPage.razor` | `@attribute [Authorize]` | Tidak (`UserManager`) | *(Panggil `UserManager.ChangePasswordAsync` langsung)* | Berfungsi |
| 5 | `/security/users` | `Security/UsersPage.razor` | ❌ **Tidak Ada** | ⚠️ Ya (`ApplicationDbContext`) | *(Panggil `UserManager` & `DbContext` langsung)* | **Kritis (`SEC-CRIT-03`):** User anonim dapat membuat user & menonaktifkan akun |
| 6 | `/security/roles` | `Security/RolesPage.razor` | ❌ **Tidak Ada** | ⚠️ Ya (`ApplicationDbContext`) | *(Mutasi `DbContext.RolePermissions` langsung)* | **Kritis (`SEC-CRIT-03`):** User anonim dapat mengubah matriks izin PBAC |
| 7 | `/security/audit-logs` | `Security/AuditLogsPage.razor` | ❌ **Tidak Ada** | ⚠️ Ya (`ApplicationDbContext`) | *(Query `DbContext.AuditLogs.Take(200)` langsung)* | **Kritis (`SEC-CRIT-03`):** Eksposur log audit tanpa autentikasi |
| 8 | `/settings` | `Settings/SettingsPage.razor` | `[Authorize(Policy = Permissions.Master.CompanyManage)]` | Tidak | *(Hanya simulasi `Task.Delay` + `Snackbar`)* | **Crash (`SEC-CRIT-02`)** & Mock UI (`ARCH-MED-02`) |
| 9 | `/master/companies` | `Master/CompaniesPage.razor` | ❌ **Tidak Ada** | Tidak | `GetCompaniesQuery`, `CreateCompanyCommand` | Halaman tidak memiliki `[Authorize]` (`SEC-CRIT-03`) |
| 10 | `/master/branches` | `Master/BranchesPage.razor` | ❌ **Tidak Ada** | ⚠️ Ya (`IApplicationDbContext`) | `GetBranchesQuery`, `CreateBranchCommand` | Halaman tidak memiliki `[Authorize]` (`SEC-CRIT-03`) |
| 11 | `/master/departments` | `Master/DepartmentsPage.razor` | ❌ **Tidak Ada** | ⚠️ Ya (`IApplicationDbContext`) | *(CRUD langsung ke `DbContext.Departments` di `.razor`!)* | Bypass MediatR & tanpa `[Authorize]` (`SEC-CRIT-03`) |
| 12 | `/master/products` | `Master/ProductsPage.razor` | ❌ **Tidak Ada** | ⚠️ Ya (`IApplicationDbContext`) | `GetProductsQuery`, `CreateProductCommand` (+ seed kategori/UoM via `DbContext`) | Halaman tidak memiliki `[Authorize]` (`SEC-CRIT-03`) |
| 13 | `/master/customers` | `Master/CustomersPage.razor` | ❌ **Tidak Ada** | ⚠️ Ya (`IApplicationDbContext`) | `GetCustomersQuery`, `CreateCustomerCommand` | Halaman tidak memiliki `[Authorize]` (`SEC-CRIT-03`) |
| 14 | `/master/suppliers` | `Master/SuppliersPage.razor` | ❌ **Tidak Ada** | ⚠️ Ya (`IApplicationDbContext`) | `GetSuppliersQuery`, `CreateSupplierCommand` | Halaman tidak memiliki `[Authorize]` (`SEC-CRIT-03`) |
| 15 | `/inventory/stock-balances` | `Inventory/StockBalancesPage.razor` | ❌ **Tidak Ada** | ⚠️ Ya (`IApplicationDbContext`) | `GetStockBalancesQuery`, `GetStockCardQuery` | Baik halaman maupun Query tidak memiliki proteksi izin (`SEC-CRIT-03`, `SEC-HIGH-01`) |
| 16 | `/inventory/transfers` | `Inventory/StockTransfersPage.razor` | ❌ **Tidak Ada** | ⚠️ Ya (`IApplicationDbContext`) | `GetStockTransfersQuery`, `Create/Dispatch/ReceiveStockTransferCommand` | Halaman & `GetStockTransfersQuery` tanpa proteksi izin |
| 17 | `/inventory/adjustments` | `Inventory/StockAdjustmentsPage.razor` | ❌ **Tidak Ada** | ⚠️ Ya (`IApplicationDbContext`) | `GetStockAdjustmentsQuery`, `Create/ApproveStockAdjustmentCommand` | Halaman & `GetStockAdjustmentsQuery` tanpa proteksi izin |
| 18 | `/procurement/requests` | `Procurement/PurchaseRequestsPage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`IApplicationDbContext`) | `GetPurchaseRequestsQuery`, `Create/Submit/Approve/RejectPurchaseRequestCommand` | `GetPurchaseRequestsQuery` tanpa `[RequirePermission]` |
| 19 | `/procurement/orders` | `Procurement/PurchaseOrdersPage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`IApplicationDbContext`) | `GetPurchaseOrdersQuery`, `Create/ApprovePurchaseOrderCommand` | `GetPurchaseOrdersQuery` tanpa `[RequirePermission]` |
| 20 | `/procurement/receipts` | `Procurement/GoodsReceiptsPage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`IApplicationDbContext`) | `GetGoodsReceiptsQuery`, `Create/ReceiveGoodsReceiptCommand` | `GetGoodsReceiptsQuery` mengembalikan `PagedResult` mentah (`ARCH-MED-01`) |
| 21 | `/procurement/bills` | `Procurement/VendorBillsPage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`IApplicationDbContext`) | `GetVendorBillsQuery`, `Create/PostVendorBillCommand` | `GetVendorBillsQuery` mengembalikan `PagedResult` mentah (`ARCH-MED-01`) |
| 22 | `/sales/quotations` | `Sales/QuotationsPage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`IApplicationDbContext`) | `GetQuotationsQuery`, `Create/Send/AcceptQuotationCommand` | Berfungsi (untuk `SUPER_ADMIN`) |
| 23 | `/sales/orders` | `Sales/SalesOrdersPage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`IApplicationDbContext`) | `GetSalesOrdersQuery`, `Create/ConfirmSalesOrderCommand` | Berfungsi (untuk `SUPER_ADMIN`) |
| 24 | `/sales/deliveries` | `Sales/DeliveriesPage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`IApplicationDbContext`) | `GetDeliveriesQuery`, `Create/DispatchDeliveryCommand` | Berfungsi (untuk `SUPER_ADMIN`) |
| 25 | `/sales/invoices` | `Sales/SalesInvoicesPage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`IApplicationDbContext`) | `GetSalesInvoicesQuery`, `Create/PostSalesInvoiceCommand` | Berfungsi (untuk `SUPER_ADMIN`) |
| 26 | `/finance/coa` | `Finance/ChartOfAccountsPage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`IApplicationDbContext`) | `GetChartOfAccountsQuery`, `CreateChartOfAccountCommand` | Berfungsi (untuk `SUPER_ADMIN`) |
| 27 | `/finance/journals` | `Finance/JournalEntriesPage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`IApplicationDbContext`) | `GetJournalEntriesQuery`, `Create/Post/ReverseJournalEntryCommand` | Berfungsi (untuk `SUPER_ADMIN`) |
| 28 | `/finance/payments` | `Finance/PaymentsPage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`IApplicationDbContext`) | `GetPaymentsQuery`, `Create/PostPaymentCommand` | Berfungsi (untuk `SUPER_ADMIN`) |
| 29 | `/finance/periods` | `Finance/PeriodClosingPage.razor` | `@attribute [Authorize]` | ⚠️ Ya (`IApplicationDbContext`) | `GetFiscalPeriodsQuery`, `Create/CloseFiscalPeriodCommand` | Berfungsi (untuk `SUPER_ADMIN`) |
| 30 | `/hr/employees` | `Hr/EmployeesPage.razor` | `[Authorize(Policy = Permissions.Hr.EmployeeManage)]` | ⚠️ Ya (`IApplicationDbContext`) | `GetEmployeesQuery`, `CreateEmployeeCommand` | **Crash saat dibuka (`SEC-CRIT-02`)** |
| 31 | `/hr/attendance` | `Hr/AttendancePage.razor` | `[Authorize(Policy = Permissions.Hr.AttendanceManage)]` | ⚠️ Ya (`IApplicationDbContext`) | `GetAttendanceRecordsQuery`, `RecordAttendanceCommand` | **Crash saat dibuka (`SEC-CRIT-02`)** |
| 32 | `/hr/leaves` | `Hr/LeavesPage.razor` | `[Authorize(Policy = Permissions.Hr.LeaveApprove)]` | ⚠️ Ya (`IApplicationDbContext`) | `GetLeaveRequestsQuery`, `Create/ApproveLeaveRequestCommand` | **Crash saat dibuka (`SEC-CRIT-02`)** |
| 33 | `/hr/payroll` | `Hr/PayrollPage.razor` | `[Authorize(Policy = Permissions.Hr.PayrollProcess)]` | ⚠️ Ya (`IApplicationDbContext`) | `GetPayrollPeriodsQuery`, `Create/Calculate/Approve/PayPayrollCommand` | **Crash saat dibuka (`SEC-CRIT-02`)** |
| 34 | `/reports/financial` | `Reports/FinancialReportPage.razor` | `[Authorize(Policy = Permissions.Reports.FinancialView)]` | ⚠️ Ya (`IApplicationDbContext`) | `GetBalanceSheetQuery`, `GetProfitAndLossQuery`, `GetTrialBalanceQuery` | **Crash saat dibuka (`SEC-CRIT-02`)** |
| 35 | `/reports/inventory` | `Reports/InventoryReportPage.razor` | `[Authorize(Policy = Permissions.Reports.FinancialView)]` | ⚠️ Ya (`IApplicationDbContext`) | `GetInventoryValuationQuery` | **Crash saat dibuka (`SEC-CRIT-02`)** |
| 36 | `/reports/sales` | `Reports/SalesReportPage.razor` | `[Authorize(Policy = Permissions.Reports.FinancialView)]` | ⚠️ Ya (`IApplicationDbContext`) | `GetSalesAnalyticsQuery` | **Crash saat dibuka (`SEC-CRIT-02`)** |

*(Catatan: Terdapat total 36 komponen `.razor` di bawah `Components/Pages/`, terdiri dari 1 Dashboard, 3 halaman Auth, 3 halaman Security, 1 halaman Settings, 6 halaman Master, 3 halaman Inventory, 4 halaman Procurement, 4 halaman Sales, 4 halaman Finance, 4 halaman HR, dan 3 halaman Reports).*

---

## 3. Alur Proses Bisnis Inti (End-to-End Business Workflows)

### 3.1 Procure-to-Pay (P2P) Workflow
1. **Purchase Request (`PurchaseRequest`):** Dibuat dengan status `Draft` (`CreatePurchaseRequestCommand`), diajukan (`SubmitPurchaseRequestCommand` -> `Submitted`), lalu disetujui/ditolak (`ApprovePurchaseRequestCommand` / `RejectPurchaseRequestCommand` -> `Approved`/`Rejected`).
2. **Purchase Order (`PurchaseOrder`):** Dibuat (`CreatePurchaseOrderCommand` -> `Draft` dengan kalkulasi Subtotal, PPN 11%, dan Total), lalu disetujui (`ApprovePurchaseOrderCommand` -> `Approved`).
3. **Goods Receipt Note (`GoodsReceipt`):**
   - Dibuat terhadap PO yang berstatus `Approved`/`PartiallyReceived` (`CreateGoodsReceiptCommand`), memvalidasi *over-receipt* (`BR-PRC-001` pada baris 90: `poItem.ReceivedQuantity + itemReq.QuantityReceived > poItem.Quantity`).
   - Saat diposting (`ReceiveGoodsReceiptCommand`), sistem memperbarui `poItem.RecordReceipt()`, mengalkulasi ulang HPP rata-rata tertimbang (**Moving Average Cost** di `StockBalance.AddStock()`), mencatat `StockMovement` bertipe `PurchaseReceipt`, dan memperbarui status PO menjadi `PartiallyReceived` atau `Completed`.
   - **Celah Integrasi (`ARCH-HIGH-02`):** Penerimaan barang belum membuat jurnal akuntansi otomatis (*Debit Persediaan / Kredit GR-IR Clearing*).
4. **Vendor Bill (`VendorBill`) & Payment (`Payment`):**
   - Faktur pemasok dibuat (`CreateVendorBillCommand`) dengan validasi batas tagihan terhadap nilai PO (`CreateVendorBillCommand.cs:54`), lalu diposting (`PostVendorBillCommand` -> `Posted`).
   - Pelunasan dilakukan di `PaymentsPage.razor` (`CreatePaymentCommand` + `PostPaymentCommand`), yang mengurangi saldo `CashBankAccount` dan memanggil `bill.ApplyPayment(alloc.AllocatedAmount)`.
   - **Celah Integrasi (`ARCH-HIGH-02`):** Posting `VendorBill` dan `Payment` belum menghasilkan `JournalEntry` otomatis di Buku Besar (`finance.journal_entries`).

### 3.2 Order-to-Cash (O2C) Workflow
1. **Sales Quotation (`Quotation`):** Dibuat (`CreateQuotationCommand` -> `Draft`), dikirim ke pelanggan (`SendQuotationCommand` -> `Sent`), dan diterima (`AcceptQuotationCommand` -> `Accepted`).
2. **Sales Order (`SalesOrder`):**
   - Saat dibuat (`CreateSalesOrderCommand`), sistem memeriksa **Credit Limit Pelanggan (`BR-SAL-001`)** dengan menjumlahkan sisa piutang faktur (`SalesInvoices` berstatus `Posted`/`PartiallyPaid`) ditambah total SO yang sedang aktif (`Confirmed`/`Processing`) dibandingkan dengan `customer.CreditLimit` (`CreateSalesOrderCommand.cs:66-85`).
   - Saat dikonfirmasi (`ConfirmSalesOrderCommand`), sistem melakukan **Hard Stock Reservation** (`stockBalance.ReserveStock(item.Quantity)`) di gudang yang dipilih.
3. **Delivery Order (`Delivery`):**
   - Dibuat terhadap SO berstatus `Confirmed`/`PartiallyDelivered` (`CreateDeliveryCommand`) dengan validasi agar kuantitas kirim tidak melebihi sisa pesanan (`CreateDeliveryCommand.cs:90`).
   - Saat dikirim (`DispatchDeliveryCommand`), sistem memanggil `stockBalance.ReleaseAndDeductReservedStock(item.QuantityShipped)`, mencatat `StockMovement` bertipe `SalesDelivery` dengan `UnitCost = stockBalance.AverageCost`, dan mengubah status SO menjadi `PartiallyDelivered` atau `Completed`.
4. **Sales Invoice (`SalesInvoice`) & Customer Receipt (`Payment`):**
   - Faktur penjualan dibuat (`CreateSalesInvoiceCommand`), diposting (`PostSalesInvoiceCommand`), dan dilunasi melalui `PostPaymentCommand` (`PaymentType.CustomerReceipt`), yang menambah saldo `CashBankAccount` dan memperbarui `invoice.ApplyPayment(...)`.
   - **Celah Integrasi (`ARCH-HIGH-02`):** Pengiriman barang (`DispatchDeliveryCommand`) belum menjurnal HPP (*Debit COGS / Kredit Persediaan*), dan posting `SalesInvoice` belum menjurnal Piutang & Pendapatan (*Debit AR / Kredit Sales Revenue & PPN Keluaran*).

### 3.3 Hire-to-Retire & Payroll Workflow
1. **Employee Onboarding (`Employee`):** Pendaftaran karyawan beserta NIK, KTP (16 digit), cabang, departemen, status kepegawaian, dan gaji pokok (`CreateEmployeeCommand`).
2. **Attendance & Leave (`AttendanceRecord`, `LeaveRequest`):** Pencatatan kehadiran harian (`RecordAttendanceCommand`) dengan pencegahan duplikasi per tanggal, serta pengajuan dan persetujuan cuti (`CreateLeaveRequestCommand`, `ApproveLeaveRequestCommand`) dengan validasi kuota tahunan `LeaveType.AnnualAllowanceDays` (`BR-HR-001`).
3. **Payroll Processing (`PayrollPeriod`, `PayrollItem`):**
   - Pembuatan periode gaji (`CreatePayrollPeriodCommand`), dilanjutkan kalkulasi batch (`CalculatePayrollCommand`) yang menghitung tunjangan tetap (10% untuk karyawan `Permanent`), potongan keterlambatan (`Rp 50.000` per kejadian terlambat > 15 menit) + BPJS 3%, dan estimasi PPh 21 (`BR-HR-002` — 5% untuk penghasilan bruto di atas PTKP Rp 4.500.000).
   - Persetujuan (`ApprovePayrollCommand`) dan pencairan gaji (`PayPayrollCommand`).
