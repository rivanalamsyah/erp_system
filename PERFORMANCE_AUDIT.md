# PERFORMANCE AUDIT — Evaluasi Performa Query, Penggunaan Memori, Konkurensi & Skalabilitas

> **Tanggal Audit:** 12 November 2026  
> **Fokus Evaluasi:** EF Core Query Execution Plan, In-Memory Materialization, N+1 Query Loops, Blazor Server Circuit Memory Footprint, Reflection Overhead, dan Caching

---

## 1. Ringkasan Temuan Performa & Skalabilitas

| ID Temuan | Judul Temuan | Severity | Lokasi Utama |
| :--- | :--- | :---: | :--- |
| **`PERF-CRIT-01`** | Materialisasi tabel penuh ke memori aplikasi (`.ToListAsync()` tanpa `.AsNoTracking()`) & iterasi $O(A \times L)$ pada Laporan Keuangan, Analitik Penjualan, dan Dashboard | 🔴 **Critical** | `GetFinancialReportsQuery.cs`, `GetSalesAnalyticsQuery.cs`, `Home.razor` |
| **`PERF-HIGH-01`** | Pola N+1 Database Query di dalam `foreach` loop pada 8 Command Handler Transaksi Inti & `UsersPage.razor` | 🟠 **High** | 8 Command Handler di `Sales`, `Inventory`, `Procurement`, `Finance` + `UsersPage.razor` |
| **`PERF-HIGH-02`** | Eksekusi 11 Query Sekuensial (Termasuk Materialisasi Seluruh `StockBalances`) pada Setiap Kunjungan Dashboard (`Home.razor`) Tanpa Caching | 🟠 **High** | `src/ERP.Web/Components/Pages/Home.razor:262-331` |
| **`PERF-MED-01`** | Materialisasi Penuh Seluruh Periode & Item Slip Gaji di Memori pada `GetPayrollPeriodsQuery` serta Ketiadaan Paginasi di Modul HR | 🟡 **Medium** | `GetPayrollPeriodsQuery.cs`, `GetEmployeesQuery.cs`, `GetLeaveRequestsQuery.cs` |
| **`PERF-MED-02`** | Overhead Refleksi (`GetType().GetProperty(...)`) Berulang pada Setiap Entitas di `AuditSaveChangesInterceptor` dan `AuthorizationPipelineBehavior` | 🟡 **Medium** | `AuditSaveChangesInterceptor.cs:28-115`, `AuthorizationPipelineBehavior.cs:15` |
| **`PERF-LOW-01`** | Query Tanpa `.AsNoTracking()` pada Sejumlah Pembacaan Dropdown & Lookup di Komponen `.razor` | 🟢 **Low** | `EmployeesPage.razor:169-173`, `LeavesPage.razor:132-133`, `InventoryReportPage.razor:129` |

---

## 2. Analisis Detail Bottleneck Performa

---

### [PERF-CRIT-01] Materialisasi Tabel Penuh ke Memori C# & Kompleksitas $O(A \times L)$ pada Laporan Keuangan dan Analitik Penjualan
- **Severity:** `Critical`
- **Lokasi File & Baris:**
  1. `src/ERP.Application/Features/Reports/GetFinancialReportsQuery.cs:29-57` (`GetBalanceSheetQueryHandler`)
  2. `src/ERP.Application/Features/Reports/GetFinancialReportsQuery.cs:91-119` (`GetProfitAndLossQueryHandler`)
  3. `src/ERP.Application/Features/Reports/GetFinancialReportsQuery.cs:149-174` (`GetTrialBalanceQueryHandler`)
  4. `src/ERP.Application/Features/Reports/GetSalesAnalyticsQuery.cs:22-75` (`GetSalesAnalyticsQueryHandler`)
- **Bukti Kode Aktual (`GetFinancialReportsQuery.cs:29-43`):**
  ```csharp
  var accounts = await _context.ChartOfAccounts
      .Where(c => c.CompanyId == request.CompanyId && !c.IsHeader && c.IsActive)
      .ToListAsync(cancellationToken);

  var lines = await _context.JournalLines
      .Include(l => l.JournalEntry)
      .Where(l => l.JournalEntry.CompanyId == request.CompanyId &&
                  l.JournalEntry.Status == JournalStatus.Posted &&
                  l.JournalEntry.JournalDate <= request.AsOfDate)
      .ToListAsync(cancellationToken);

  foreach (var acc in accounts)
  {
      var accLines = lines.Where(l => l.AccountId == acc.Id).ToList();
      var debit = accLines.Sum(l => l.Debit);
      var credit = accLines.Sum(l => l.Credit);
      ...
  ```
  Dan pada `GetSalesAnalyticsQuery.cs:22-71`:
  ```csharp
  var invoices = await invQuery.ToListAsync(cancellationToken); // Menarik seluruh SalesInvoice + Customer (Tracked!)
  var soItems = await soQuery.ToListAsync(cancellationToken);   // Menarik seluruh SalesOrderItem + SalesOrder + Product (Tracked!)
  ```
- **Akar Masalah (Root Cause):**
  1. Query **tidak melakukan agregasi `GROUP BY` dan `SUM` di PostgreSQL**, melainkan menarik **seluruh baris `JournalLines` beserta `JournalEntry`** sejak awal perusahaan berdiri hingga `AsOfDate` ke memori RAM aplikasi .NET!
  2. Query tersebut bahkan **tidak menggunakan `.AsNoTracking()`**, sehingga EF Core membuat *snapshot* pelacakan perubahan (`ChangeTracker`) untuk ratusan ribu baris `JournalLine`, `JournalEntry`, `SalesInvoice`, dan `SalesOrderItem` di dalam memori Blazor Circuit!
  3. Setelah semua baris berada di memori, kode melakukan iterasi bersarang `foreach (var acc in accounts) { lines.Where(l => l.AccountId == acc.Id)... }` dengan kompleksitas waktu $O(A \times L)$ di mana $A$ adalah jumlah akun COA dan $L$ adalah jumlah baris jurnal.
- **Dampak Sistem:**
  - Pada skala produksi (misal 100.000 baris jurnal per tahun), membuka halaman `/reports/financial` akan mengalokasikan ratusan megabyte memori per pengguna, membebani Garbage Collector (Gen 2 / LOH), dan memakan waktu beberapa detik hingga memicu `OutOfMemoryException`.
- **Rekomendasi Perbaikan Teknis:**
  - Dorong seluruh perhitungan agregasi ke sisi database PostgreSQL menggunakan proyeksi LINQ `GroupBy(l => l.AccountId).Select(g => new { AccountId = g.Key, TotalDebit = g.Sum(x => x.Debit), TotalCredit = g.Sum(x => x.Credit) })` dengan `.AsNoTracking()`. Database hanya akan mengembalikan maksimal ~100 baris ringkasan per akun ke aplikasi!
  - Demikian pula pada `GetSalesAnalyticsQuery`, gunakan query `SumAsync`, `CountAsync`, dan `GroupBy(...).OrderByDescending(...).Take(5)` langsung di SQL.

---

### [PERF-HIGH-01] Query Database N+1 di Dalam Loop pada 8 Command Handler Transaksi & `UsersPage.razor`
- **Severity:** `High`
- **Lokasi File & Baris:**
  1. `ConfirmSalesOrderCommandHandler` (`src/ERP.Application/Features/Sales/SalesOrders/CreateSalesOrderCommand.cs:142-157`):
     Memanggil `await context.StockBalances.FirstOrDefaultAsync(...)` di dalam `foreach (var item in order.Items)`.
  2. `DispatchStockTransferCommandHandler` (`src/ERP.Application/Features/Inventory/StockTransfers/CreateStockTransferCommand.cs:99-128`):
     Memanggil `await context.StockBalances.FirstOrDefaultAsync(...)` di dalam `foreach (var item in transfer.Items)`.
  3. `ReceiveStockTransferCommandHandler` (`src/ERP.Application/Features/Inventory/StockTransfers/CreateStockTransferCommand.cs:156-192`):
     Memanggil **2 query** (`StockBalances.FirstOrDefaultAsync` untuk gudang asal dan gudang tujuan) di dalam setiap iterasi `foreach (var item in transfer.Items)`.
  4. `CreateStockAdjustmentCommandHandler` & `ApproveStockAdjustmentCommandHandler` (`src/ERP.Application/Features/Inventory/StockAdjustments/CreateStockAdjustmentCommand.cs:59-68, 96-147`):
     Memanggil `StockBalances.FirstOrDefaultAsync` di dalam loop item.
  5. `ReceiveGoodsReceiptCommandHandler` (`src/ERP.Application/Features/Procurement/GoodsReceipts/CreateGoodsReceiptCommand.cs:134-165`):
     Memanggil `StockBalances.FirstOrDefaultAsync` di dalam loop item GRN.
  6. `DispatchDeliveryCommandHandler` (`src/ERP.Application/Features/Sales/Deliveries/CreateDeliveryCommand.cs:133-172`):
     Memanggil `StockBalances.FirstOrDefaultAsync` di dalam loop item pengiriman.
  7. `PostPaymentCommandHandler` (`src/ERP.Application/Features/Finance/Payments/CreatePaymentCommand.cs:112-149`):
     Memanggil `SalesInvoices.FirstOrDefaultAsync` atau `VendorBills.FirstOrDefaultAsync` di dalam `foreach (var alloc in payment.Allocations)`.
  8. `UsersPage.razor` (`src/ERP.Web/Components/Pages/Security/UsersPage.razor:161-166`):
     Memanggil `await UserManager.GetRolesAsync(u)` di dalam `foreach (var u in rawUsers)`.
- **Dampak Sistem:**
  - Dokumen PO/SO/Transfer/Opname dengan 50 baris item akan mengeksekusi 50–100 *round-trip* SQL terpisah ke PostgreSQL dalam satu transaksi, meningkatkan latensi penguncian baris (*row lock duration*) dan potensi *deadlock*.
- **Rekomendasi Perbaikan Teknis:**
  - Kumpulkan seluruh `productIds` terlebih dahulu (`var productIds = items.Select(i => i.ProductId).Distinct().ToList()`), lalu muat seluruh `StockBalance` terkait dalam **satu query batch** (`await context.StockBalances.Where(sb => sb.WarehouseId == warehouseId && productIds.Contains(sb.ProductId)).ToDictionaryAsync(sb => sb.ProductId, cancellationToken)`).
  - Untuk `UsersPage.razor`, lakukan *join* antara `Users`, `UserRoles`, dan `Roles` dalam satu query proyeksi EF Core.

---

### [PERF-HIGH-02] Eksekusi 11 Query Sekuensial & Materialisasi Seluruh `StockBalances` pada Dashboard (`Home.razor`)
- **Severity:** `High`
- **Lokasi File & Baris:**
  - `src/ERP.Web/Components/Pages/Home.razor:262-331`
- **Bukti Kode Aktual (`Home.razor:276-279`):**
  ```csharp
  var stockBalances = await DbContext.StockBalances.Include(b => b.Product).ToListAsync();
  _totalStockValue = stockBalances.Sum(b => b.PhysicalQuantity * (b.AverageCost > 0 ? b.AverageCost : (b.Product?.PurchasePrice ?? 0)));
  _lowStockCount = stockBalances.Count(b => b.Product != null && b.PhysicalQuantity <= b.Product.MinimumStockLevel);
  ```
- **Akar Masalah & Dampak:**
  - Setiap kali pengguna membuka halaman utama (`/`), `Home.razor` mengeksekusi 11 query database secara berurutan, dan salah satunya menarik **seluruh baris tabel `inventory.stock_balances` beserta `master.products` ke memori C#** hanya untuk menghitung nilai total persediaan (`_totalStockValue`) dan jumlah item stok menipis (`_lowStockCount`).
- **Rekomendasi Perbaikan Teknis:**
  - Pindahkan pembacaan statistik Dashboard ke satu Query Handler (`GetDashboardSummaryQuery`) yang menghitung agregasi langsung di SQL (`SumAsync` dan `CountAsync`), serta terapkan `IMemoryCache` / `HybridCache` dengan TTL pendek (misal 30–60 detik) per `CompanyId`.

---

### [PERF-MED-01] Materialisasi Penuh di `GetPayrollPeriodsQuery` & Ketiadaan Paginasi pada Query HR
- **Severity:** `Medium`
- **Lokasi File & Baris:**
  - `src/ERP.Application/Features/Hr/Payroll/GetPayrollPeriodsQuery.cs:24-65`
  - `src/ERP.Application/Features/Hr/Employees/GetEmployeesQuery.cs:24-62`
  - `src/ERP.Application/Features/Hr/Leaves/GetLeaveRequestsQuery.cs:24-54`
- **Akar Masalah & Dampak:**
  - `GetPayrollPeriodsQuery` memuat **seluruh periode penggajian sepanjang sejarah** beserta **seluruh item slip gaji karyawan (`Items`)**, `Employee`, dan `Department` sekaligus ke memori menggunakan `.ToListAsync()` sebelum memetakan ke `PayrollPeriodDto`.
  - `GetEmployeesQuery` dan `GetLeaveRequestsQuery` mengembalikan `List<T>` penuh tanpa parameter `PageNumber` dan `PageSize`.
- **Rekomendasi Perbaikan Teknis:**
  - Pisahkan query daftar ringkasan periode gaji (`GetPayrollPeriodsQuery` dengan proyeksi SQL `Items.Sum(...)` dan `Items.Count()`) dari query detail slip gaji per periode (`GetPayrollPeriodItemsQuery(Guid PeriodId)`), serta tambahkan paginasi `PagedResult<T>` pada query HR.

---

### [PERF-MED-02] Overhead Refleksi Berulang di `AuditSaveChangesInterceptor` dan `AuthorizationPipelineBehavior`
- **Severity:** `Medium`
- **Lokasi File & Baris:**
  - `src/ERP.Infrastructure/Persistence/Interceptors/AuditSaveChangesInterceptor.cs:37-115`
  - `src/ERP.Application/Common/Behaviors/AuthorizationPipelineBehavior.cs:15`
- **Akar Masalah & Dampak:**
  - Meskipun `BaseEntity` telah mengimplementasikan interface `IAuditableEntity`, `ISoftDeletableEntity`, dan `IMustHaveTenant` (`src/ERP.Domain/Common/BaseEntity.cs:11-28`), `AuditSaveChangesInterceptor` tidak melakukan *type-casting* ke interface tersebut pada bagian audit log, melainkan memanggil `entityType.GetProperty("CreatedAtUtc")`, `GetProperty("Id")`, `GetProperty("CompanyId")` berulang kali menggunakan refleksi untuk setiap entitas yang berubah.
  - Selain itu, pada entitas berstatus `EntityState.Added`, `AuditSaveChangesInterceptor` berjalan pada tahap `SavingChanges` (sebelum perintah `INSERT`), lalu langsung menambahkan `AuditLog` ke `context.AuditLogs.AddRange(auditEntries)`.
- **Rekomendasi Perbaikan Teknis:**
  - Gunakan pattern matching langsung terhadap interface (`if (entry.Entity is IMustHaveTenant tenantEntity)`) atau properti `entry.Property("CompanyId").CurrentValue` milik EF Core metadata yang sudah di-cache tanpa refleksi `System.Reflection` manual, serta simpan cache `ConcurrentDictionary<Type, IReadOnlyList<RequirePermissionAttribute>>` di `AuthorizationPipelineBehavior`.

