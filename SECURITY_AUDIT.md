# SECURITY AUDIT — Evaluasi Mendalam Autentikasi, Otorisasi, PBAC & Keamanan Aplikasi

> **Tanggal Audit:** 12 November 2026  
> **Fokus Evaluasi:** ASP.NET Core Identity, Cookie & Claims Hydration, Permission-Based Access Control (PBAC), Multi-Tenant Data Isolation, CSRF, Rate Limiting, & Secrets Management

---

## 1. Ringkasan Eksekutif Keamanan

Arsitektur keamanan aplikasi dirancang menggunakan **ASP.NET Core Identity** dengan penyimpanan kustom untuk matriks izin (`identity.role_permissions`) dan otorisasi cabang (`identity.user_branches`), dipadukan dengan `AuthorizationPipelineBehavior` pada MediatR dan Global Query Filter (`IMustHaveTenant`) pada EF Core.

Namun, verifikasi faktual terhadap kode sumber mengungkap **3 kerentanan tingkat `Critical`**, **2 kerentanan tingkat `High`**, dan **4 kerentanan tingkat `Medium`** yang menyebabkan mekanisme otorisasi dan isolasi multi-tenant **tidak berjalan sebagaimana mestinya saat runtime**.

| ID Temuan | Judul Temuan | Severity | Komponen Terdampak |
| :--- | :--- | :---: | :--- |
| **`SEC-CRIT-01`** | Ketiadaan `IUserClaimsPrincipalFactory<ApplicationUser>` menyebabkan klaim `Permission`, `CompanyId`, `BranchId`, dan `FullName` tidak pernah dihidrasi saat login (mengunci seluruh role non-SuperAdmin & melumpuhkan Global Query Filter Multi-Tenant) | 🔴 **Critical** | `DependencyInjection.cs`, `CurrentUserService.cs`, `ApplicationDbContext.cs` |
| **`SEC-CRIT-02`** | 8 Halaman Blazor (`Settings`, 4 Halaman `HR`, 3 Halaman `Reports`) menggunakan `[Authorize(Policy = ...)]` tanpa registrasi Policy di DI, memicu Runtime Crash `InvalidOperationException` | 🔴 **Critical** | `Program.cs`, 8 file `.razor` |
| **`SEC-CRIT-03`** | 13 Halaman UI (termasuk `/security/users`, `/security/roles`, `/security/audit-logs`, `/`, `/master/*`, `/inventory/*`) tidak memiliki `@attribute [Authorize]` dan memanipulasi `DbContext`/`UserManager` secara langsung | 🔴 **Critical** | 13 file `.razor` di `Components/Pages/` |
| **`SEC-HIGH-01`** | 18 Request MediatR (Commands & Queries di HR, Reports, Inventory, dan Procurement) tidak memiliki atribut `[RequirePermission]` sehingga melewati pemeriksaan autentikasi & otorisasi pipeline | 🟠 **High** | 18 Command/Query di `ERP.Application` |
| **`SEC-HIGH-02`** | Rate Limiter pada `/api/auth/login` menggunakan *Global Fixed Window* tanpa partisi IP, memungkinkan *Denial of Service (DoS)* login seluruh perusahaan hanya dengan 5 request gagal | 🟠 **High** | `src/ERP.Web/Program.cs:22-32` |
| **`SEC-MED-01`** | Endpoint `/api/auth/logout` dan form keluar di `MainLayout.razor` tidak menggunakan validasi token Anti-CSRF (`<AntiforgeryToken />`) | 🟡 **Medium** | `MainLayout.razor:115`, `Program.cs:114` |
| **`SEC-MED-02`** | Kredensial default `SUPER_ADMIN` (`admin@enterprise.erp / Admin@12345`) ditampilkan secara terbuka di halaman publik `Login.razor` dan kredensial DB plaintext di `appsettings.json` | 🟡 **Medium** | `Login.razor:66-68`, `appsettings.json:3` |
| **`SEC-MED-03`** | Urutan registrasi MediatR Pipeline menjalankan `ValidationPipelineBehavior` sebelum `AuthorizationPipelineBehavior` | 🟡 **Medium** | `src/ERP.Application/DependencyInjection.cs:20-21` |
| **`SEC-MED-04`** | `AuditSaveChangesInterceptor` tidak pernah mengisi properti `IpAddress` pada `AuditLog` | 🟡 **Medium** | `AuditSaveChangesInterceptor.cs:116-127` |

---

## 2. Analisis Detail Temuan Keamanan

---

### [SEC-CRIT-01] Ketiadaan `IUserClaimsPrincipalFactory<ApplicationUser>` — Klaim `Permission`, `CompanyId`, `BranchId`, dan `AllowedBranchId` Tidak Pernah Dihidrasi Saat Login
- **Severity:** `Critical`
- **Lokasi File & Baris:**
  - `src/ERP.Infrastructure/DependencyInjection.cs:39-52`
  - `src/ERP.Infrastructure/Services/CurrentUserService.cs:24-64`
  - `src/ERP.Infrastructure/Persistence/Seeders/DatabaseSeeder.cs:175-349`
  - `src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs:163, 176, 189, 240, 258, 285, 345, 368, 412, 439, 466, 488, 513, 535, 560, 574, 598, 623, 646, 677, 697`
- **Bukti Mekanisme Aktual:**
  1. Di `CurrentUserService.cs:24-64`, layanan membaca identitas dan izin pengguna dari `ClaimsPrincipal`:
     - `CompanyId` dibaca dari klaim `"CompanyId"` (baris 29).
     - `BranchId` dibaca dari klaim `"BranchId"` (baris 37).
     - `AllowedBranchIds` dibaca dari klaim `"AllowedBranchId"` (baris 45).
     - `HasPermission(string permission)` memeriksa `if (IsInRole(Roles.SuperAdmin)) return true;` lalu memeriksa `User?.HasClaim(c => c.Type == "Permission" && c.Value == permission)` (baris 61–62).
  2. Namun, di `DatabaseSeeder.cs:175-349`, daftar izin per role disimpan ke tabel kustom `context.RolePermissions` (`identity.role_permissions`), **bukan** ke tabel standar ASP.NET Core Identity `identity.role_claims` (`AspNetRoleClaims`). Demikian pula `CompanyId`, `DefaultBranchId`, dan `FullName` disimpan sebagai kolom properti pada `ApplicationUser` (`IdentityModels.cs:10-13`), serta cabang yang diizinkan disimpan di tabel `identity.user_branches`.
  3. Di `src/ERP.Infrastructure/DependencyInjection.cs:39-52`, registrasi Identity hanya memanggil:
     ```csharp
     services.AddIdentity<ApplicationUser, ApplicationRole>(options => { ... })
         .AddEntityFrameworkStores<ApplicationDbContext>()
         .AddDefaultTokenProviders();
     ```
     **Tidak ada implementasi atau registrasi `IUserClaimsPrincipalFactory<ApplicationUser>` kustom** di seluruh solusi!
- **Dampak Sistem:**
  1. **Total Lockout untuk Non-SuperAdmin:** Saat pengguna dengan role `DIRECTOR`, `FINANCE_MANAGER`, `SALES_MANAGER`, `PROCUREMENT_OFFICER`, `WAREHOUSE_STAFF`, `HR_MANAGER`, `CASHIER`, `AUDITOR`, atau `STAFF` login melalui `/api/auth/login`, `SignInManager` hanya membuat klaim standar (`NameIdentifier`, `Name`, `Role`). Klaim `"Permission"` berjumlah **0**. Akibatnya:
     - Di `NavMenu.razor:226-231`, fungsi `CanAccess(...)` selalu mengembalikan `false`, sehingga sidebar menu kosong (kecuali Dashboard dan Profil).
     - Di `AuthorizationPipelineBehavior.cs:30`, `currentUserService.HasPermission(permission)` selalu mengembalikan `false`, menolak seluruh aksi pengguna non-SuperAdmin dengan `"Anda tidak memiliki hak akses (izin: ...) untuk melakukan operasi ini."`
  2. **Lumpuhnya Isolasi Multi-Tenant (`IMustHaveTenant`):** Karena klaim `"CompanyId"` tidak pernah ada di dalam *cookie*, `CurrentUserService.CompanyId` selalu mengembalikan `Guid.Empty`. Di `ApplicationDbContext.cs`, seluruh 21 Global Query Filter ditulis dengan logika:
     ```csharp
     b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
     ```
     Karena `currentUserService.CompanyId == Guid.Empty` selalu bernilai `true` untuk semua pengguna yang login, filter isolasi `x.CompanyId == currentUserService.CompanyId` **tidak pernah aktif sama sekali**!
- **Rekomendasi Perbaikan Teknis:**
  1. Implementasikan `ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>` di `ERP.Infrastructure` yang saat login memuat:
     - Klaim `"CompanyId"` dari `user.CompanyId`
     - Klaim `"BranchId"` dari `user.DefaultBranchId`
     - Klaim `"FullName"` dari `user.FullName`
     - Seluruh klaim `"AllowedBranchId"` dari tabel `identity.user_branches`
     - Seluruh klaim `"Permission"` yang dimiliki oleh role pengguna dari tabel `identity.role_permissions`
  2. Daftarkan melalui `.AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()` pada `DependencyInjection.cs`.

---

### [SEC-CRIT-02] Unhandled Runtime Crash pada 8 Halaman Blazor Akibat Policy Otorisasi Tidak Terdaftar di `Program.cs`
- **Severity:** `Critical`
- **Lokasi File & Baris:**
  - `src/ERP.Web/Program.cs:40` (`builder.Services.AddAuthorization();`)
  - `src/ERP.Web/Components/Pages/Settings/SettingsPage.razor:2` (`@attribute [Authorize(Policy = Permissions.Master.CompanyManage)]`)
  - `src/ERP.Web/Components/Pages/Hr/EmployeesPage.razor:2` (`@attribute [Authorize(Policy = Permissions.Hr.EmployeeManage)]`)
  - `src/ERP.Web/Components/Pages/Hr/AttendancePage.razor:2` (`@attribute [Authorize(Policy = Permissions.Hr.AttendanceManage)]`)
  - `src/ERP.Web/Components/Pages/Hr/LeavesPage.razor:2` (`@attribute [Authorize(Policy = Permissions.Hr.LeaveApprove)]`)
  - `src/ERP.Web/Components/Pages/Hr/PayrollPage.razor:2` (`@attribute [Authorize(Policy = Permissions.Hr.PayrollProcess)]`)
  - `src/ERP.Web/Components/Pages/Reports/FinancialReportPage.razor:2` (`@attribute [Authorize(Policy = Permissions.Reports.FinancialView)]`)
  - `src/ERP.Web/Components/Pages/Reports/InventoryReportPage.razor:2` (`@attribute [Authorize(Policy = Permissions.Reports.FinancialView)]`)
  - `src/ERP.Web/Components/Pages/Reports/SalesReportPage.razor:2` (`@attribute [Authorize(Policy = Permissions.Reports.FinancialView)]`)
- **Akar Masalah (Root Cause):**
  Kedelapan halaman tersebut menggunakan atribut `[Authorize(Policy = "...")]` dengan nama konstanta izin dari `Permissions.*`. Namun di `Program.cs:40`, `builder.Services.AddAuthorization()` dipanggil tanpa mendaftarkan policy satu per satu maupun mendaftarkan `IAuthorizationPolicyProvider` dinamis.
- **Dampak Sistem:**
  - Setiap kali pengguna (bahkan `SUPER_ADMIN`) mengklik menu **Karyawan**, **Presensi**, **Cuti**, **Payroll**, **Laporan Finansial**, **Valuasi Stok**, **Analitik Penjualan**, atau **Pengaturan Sistem**, `AuthorizeRouteView` memanggil `DefaultAuthorizationPolicyProvider.GetPolicyAsync(policyName)` yang mengembalikan `null`, dan ASP.NET Core langsung melempar `InvalidOperationException: The AuthorizationPolicy named: '...' was not found`, mematikan koneksi Blazor aplikasi.
- **Rekomendasi Perbaikan Teknis:**
  - Implementasikan `PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider` dan `PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>` yang secara dinamis mengevaluasi policy berbasis kode permission (termasuk *bypass* otomatis untuk `Roles.SuperAdmin` atau pencocokan klaim `"Permission"`), lalu daftarkan di `Program.cs`.

---

### [SEC-CRIT-03] 13 Halaman UI (Termasuk Halaman Manajemen Pengguna, Role PBAC & Audit Log) Tidak Memiliki `@attribute [Authorize]`
- **Severity:** `Critical`
- **Lokasi File & Baris:**
  1. `src/ERP.Web/Components/Pages/Security/UsersPage.razor:1` (`/security/users`)
  2. `src/ERP.Web/Components/Pages/Security/RolesPage.razor:1` (`/security/roles`)
  3. `src/ERP.Web/Components/Pages/Security/AuditLogsPage.razor:1` (`/security/audit-logs`)
  4. `src/ERP.Web/Components/Pages/Home.razor:1` (`/`)
  5. `src/ERP.Web/Components/Pages/Master/CompaniesPage.razor:1` (`/master/companies`)
  6. `src/ERP.Web/Components/Pages/Master/BranchesPage.razor:1` (`/master/branches`)
  7. `src/ERP.Web/Components/Pages/Master/DepartmentsPage.razor:1` (`/master/departments`)
  8. `src/ERP.Web/Components/Pages/Master/ProductsPage.razor:1` (`/master/products`)
  9. `src/ERP.Web/Components/Pages/Master/CustomersPage.razor:1` (`/master/customers`)
  10. `src/ERP.Web/Components/Pages/Master/SuppliersPage.razor:1` (`/master/suppliers`)
  11. `src/ERP.Web/Components/Pages/Inventory/StockBalancesPage.razor:1` (`/inventory/stock-balances`)
  12. `src/ERP.Web/Components/Pages/Inventory/StockTransfersPage.razor:1` (`/inventory/transfers`)
  13. `src/ERP.Web/Components/Pages/Inventory/StockAdjustmentsPage.razor:1` (`/inventory/adjustments`)
- **Akar Masalah (Root Cause):**
  - `src/ERP.Web/Components/_Imports.razor` tidak mendeklarasikan `@attribute [Authorize]` secara global.
  - Ketiga belas halaman di atas lupa menyertakan `@attribute [Authorize]`.
  - Parahnya lagi, `UsersPage.razor`, `RolesPage.razor`, `AuditLogsPage.razor`, `DepartmentsPage.razor`, dan `Home.razor` **tidak menggunakan MediatR**, melainkan memanggil `UserManager<ApplicationUser>` dan `ApplicationDbContext` secara langsung di dalam blok `@code`.
- **Dampak Sistem:**
  - Pengunjung anonim (tanpa login sama sekali) yang mengakses URL `/security/users` atau `/security/roles` secara langsung dapat melihat seluruh daftar pengguna, membuat akun baru dengan role `SUPER_ADMIN` (`UsersPage.razor:217-244`), menonaktifkan akun admin (`UsersPage.razor:248-256`), atau menghapus seluruh hak akses role di `RolesPage.razor:157-189`!
- **Rekomendasi Perbaikan Teknis:**
  - Tambahkan `@attribute [Authorize]` secara global di `src/ERP.Web/Components/_Imports.razor` (sehingga *secure-by-default*, kecuali halaman yang secara eksplisit diberi `@attribute [AllowAnonymous]` seperti `Login.razor`).
  - Tambahkan `@attribute [Authorize(Policy = ...)]` spesifik pada setiap halaman modul (khususnya `Permissions.Security.UserManage`, `Permissions.Security.RoleManage`, dan `Permissions.Security.AuditView` pada halaman Security), serta pindahkan logika mutasi `UsersPage` dan `RolesPage` ke Command Handler yang terproteksi.

---

### [SEC-HIGH-01] 18 MediatR Commands & Queries Tidak Memiliki Atribut `[RequirePermission]`
- **Severity:** `High`
- **Lokasi File & Baris:**
  - **Modul HR (8 Command tanpa `[RequirePermission]`):**
    1. `CreateEmployeeCommand` (`src/ERP.Application/Features/Hr/Employees/CreateEmployeeCommand.cs:10`)
    2. `RecordAttendanceCommand` (`src/ERP.Application/Features/Hr/Attendance/RecordAttendanceCommand.cs:10`)
    3. `CreateLeaveRequestCommand` (`src/ERP.Application/Features/Hr/Leaves/CreateLeaveRequestCommand.cs:10`)
    4. `ApproveLeaveRequestCommand` (`src/ERP.Application/Features/Hr/Leaves/ApproveLeaveRequestCommand.cs:9`)
    5. `CreatePayrollPeriodCommand` (`src/ERP.Application/Features/Hr/Payroll/CreatePayrollPeriodCommand.cs:10`)
    6. `CalculatePayrollCommand` (`src/ERP.Application/Features/Hr/Payroll/CalculatePayrollCommand.cs:9`)
    7. `ApprovePayrollCommand` (`src/ERP.Application/Features/Hr/Payroll/ApprovePayrollCommand.cs:9`)
    8. `PayPayrollCommand` (`src/ERP.Application/Features/Hr/Payroll/PayPayrollCommand.cs:9`)
  - **Modul Reports (5 Query tanpa `[RequirePermission]`):**
    9. `GetBalanceSheetQuery` (`src/ERP.Application/Features/Reports/GetFinancialReportsQuery.cs:9`)
    10. `GetProfitAndLossQuery` (`src/ERP.Application/Features/Reports/GetFinancialReportsQuery.cs:10`)
    11. `GetTrialBalanceQuery` (`src/ERP.Application/Features/Reports/GetFinancialReportsQuery.cs:11`)
    12. `GetInventoryValuationQuery` (`src/ERP.Application/Features/Reports/GetInventoryValuationQuery.cs:8`)
    13. `GetSalesAnalyticsQuery` (`src/ERP.Application/Features/Reports/GetSalesAnalyticsQuery.cs:9`)
  - **Modul Inventory & Procurement (5 Query tanpa `[RequirePermission]`):**
    14. `GetWarehousesQuery` (`src/ERP.Application/Features/Inventory/Warehouses/GetWarehousesQuery.cs:8`)
    15. `GetStockBalancesQuery` & `GetStockCardQuery` (`src/ERP.Application/Features/Inventory/StockBalances/GetStockBalancesQuery.cs:8, 69`)
    16. `GetStockTransfersQuery` (`src/ERP.Application/Features/Inventory/StockTransfers/GetStockTransfersQuery.cs:8`)
    17. `GetStockAdjustmentsQuery` (`src/ERP.Application/Features/Inventory/StockAdjustments/GetStockAdjustmentsQuery.cs:8`)
    18. `GetPurchaseRequestsQuery` (`src/ERP.Application/Features/Procurement/PurchaseRequests/GetPurchaseRequestsQuery.cs:8`) & `GetPurchaseOrdersQuery` (`src/ERP.Application/Features/Procurement/PurchaseOrders/GetPurchaseOrdersQuery.cs:8`)
- **Akar Masalah (Root Cause):**
  - Di `AuthorizationPipelineBehavior.cs:15-17`, kode memeriksa:
    ```csharp
    var permissionAttributes = request.GetType().GetCustomAttributes<RequirePermissionAttribute>(true).ToList();
    if (permissionAttributes.Count != 0) { ... }
    return await next();
    ```
    Apabila sebuah Command/Query tidak memiliki atribut `[RequirePermission]`, `AuthorizationPipelineBehavior` tidak memeriksa apakah pengguna sudah terautentikasi (`IsAuthenticated`) maupun izinnya.
- **Rekomendasi Perbaikan Teknis:**
  1. Tambahkan atribut `[RequirePermission(Permissions.*)]` yang sesuai pada ke-18 Command/Query di atas.
  2. Ubah `AuthorizationPipelineBehavior` agar tetap mewajibkan `currentUserService.IsAuthenticated` secara default untuk semua request MediatR (kecuali diberi atribut `[AllowAnonymousRequest]`).

---

### [SEC-HIGH-02] Rate Limiter Global Tanpa Partisi IP pada `/api/auth/login` Memungkinkan Login DoS
- **Severity:** `High`
- **Lokasi File & Baris:**
  - `src/ERP.Web/Program.cs:22-32`
- **Bukti Kode Aktual:**
  ```csharp
  builder.Services.AddRateLimiter(options =>
  {
      options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
      options.AddFixedWindowLimiter("LoginPolicy", opt =>
      {
          opt.Window = TimeSpan.FromMinutes(1);
          opt.PermitLimit = 5;
          opt.QueueLimit = 0;
          opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
      });
  });
  ```
- **Akar Masalah (Root Cause):**
  `options.AddFixedWindowLimiter("LoginPolicy", ...)` membuat **satu kuota global tunggal (5 request/menit)** yang dibagi oleh seluruh pengguna dari seluruh alamat IP.
- **Dampak Sistem:**
  - Jika ada 5 percobaan login dalam 1 menit dari satu pengguna atau bot, seluruh karyawan lain di perusahaan yang mencoba login akan ditolak dengan HTTP `429 Too Many Requests`.
- **Rekomendasi Perbaikan Teknis:**
  - Gunakan `options.AddPolicy("LoginPolicy", httpContext => RateLimitPartition.GetFixedWindowLimiter(partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", ...))` agar batas 5 percobaan/menit dihitung per alamat IP klien.

---

### [SEC-MED-01] Endpoint `/api/auth/logout` Tanpa Validasi Anti-CSRF
- **Severity:** `Medium`
- **Lokasi File & Baris:**
  - `src/ERP.Web/Components/Layout/MainLayout.razor:115-121`
  - `src/ERP.Web/Program.cs:114-118`
- **Akar Masalah & Dampak:**
  Form `<form action="/api/auth/logout" method="post">` di `MainLayout.razor` tidak menyertakan `<AntiforgeryToken />`, dan endpoint `/api/auth/logout` tidak memvalidasi antiforgery token. Situs pihak ketiga dapat memaksa pengguna yang sedang aktif untuk *logout* (*Cross-Site Request Forgery Logout*).

### [SEC-MED-02] Eksposur Kredensial SuperAdmin di Halaman Login Publik & Plaintext Password di `appsettings.json`
- **Severity:** `Medium`
- **Lokasi File & Baris:**
  - `src/ERP.Web/Components/Pages/Auth/Login.razor:66-68`
  - `src/ERP.Web/appsettings.json:3`
- **Akar Masalah & Dampak:**
  - `Login.razor` menampilkan `Kredensial Bawaan: admin@enterprise.erp / Admin@12345` secara eksplisit di halaman publik.
  - `appsettings.json` menyertakan `Password=erp_admin;Include Error Detail=true`. `Include Error Detail=true` pada Npgsql membocorkan detail skema, nama constraint, dan parameter SQL pada pesan exception di lingkungan produksi.

### [SEC-MED-03] Urutan Pipeline Behavior Menjalankan Validasi Sebelum Otorisasi
- **Severity:** `Medium`
- **Lokasi File & Baris:**
  - `src/ERP.Application/DependencyInjection.cs:20-21`
- **Akar Masalah & Dampak:**
  `ValidationPipelineBehavior` didaftarkan sebelum `AuthorizationPipelineBehavior`, sehingga pemanggil yang tidak memiliki hak akses masih dapat memicu eksekusi validator sebelum ditolak oleh layer otorisasi.

### [SEC-MED-04] `AuditSaveChangesInterceptor` Tidak Mengisi Alamat IP Klien (`IpAddress`)
- **Severity:** `Medium`
- **Lokasi File & Baris:**
  - `src/ERP.Infrastructure/Persistence/Interceptors/AuditSaveChangesInterceptor.cs:116-127`
  - `src/ERP.Web/Components/Pages/Security/AuditLogsPage.razor:52`
- **Akar Masalah & Dampak:**
  Objek `AuditLog` yang dibuat di `AuditSaveChangesInterceptor` tidak mengisi properti `IpAddress`, sehingga di `AuditLogsPage.razor` kolom IP selalu jatuh ke nilai fallback `"127.0.0.1"`.

