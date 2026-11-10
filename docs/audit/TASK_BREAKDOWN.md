# Detailed Task Breakdown & Engineering Specifications

> **Document ID:** AUD-TSK-2026-001  
> **Related Documents:** [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md) | [ACCEPTANCE_CRITERIA.md](./ACCEPTANCE_CRITERIA.md)

---

## 1. Fase 1: Keamanan & Perbaikan Kritis (P0)

### `TSK-SEC-01`: Remediasi Antiforgery pada Endpoint Login
- **Tujuan:** Menutup celah CSRF pada proses autentikasi web.
- **Langkah Implementasi:**
  1. Hapus `.DisableAntiforgery()` pada endpoint `/api/auth/login` di `Program.cs`.
  2. Tambahkan token antiforgery pada halaman `Login.razor` menggunakan tag `<AntiforgeryToken />`.
  3. Konfigurasikan opsi cookie secure pada middleware antiforgery.
- **File Terdampak:**
  - [`src/ERP.Web/Program.cs`](file:///d:/erp_system/src/ERP.Web/Program.cs)
  - [`src/ERP.Web/Components/Pages/Auth/Login.razor`](file:///d:/erp_system/src/ERP.Web/Components/Pages/Auth/Login.razor)
- **Dependensi:** Tidak ada
- **Prioritas:** **P0 (Kritis)** | **Effort:** 4 Jam | **Status:** Not Started
- **Metode Verifikasi:** Uji submit login tanpa token CSRF (harus ditolak dengan HTTP 400 Bad Request) dan dengan token valid (harus sukses redirect ke dashboard).

---

### `TSK-SEC-02`: Eliminasi Fallback Plaintext Connection String
- **Tujuan:** Menjamin tidak ada password database yang tersimpan di kode sumber.
- **Langkah Implementasi:**
  1. Hapus string fallback hardcoded di `AddInfrastructureServices`.
  2. Tambahkan throw exception `InvalidOperationException` jika connection string tidak ditemukan di environment.
- **File Terdampak:**
  - [`src/ERP.Infrastructure/DependencyInjection.cs`](file:///d:/erp_system/src/ERP.Infrastructure/DependencyInjection.cs)
- **Dependensi:** Tidak ada
- **Prioritas:** **P1 (Tinggi)** | **Effort:** 2 Jam | **Status:** Not Started
- **Metode Verifikasi:** Jalankan aplikasi tanpa environment variable `ConnectionStrings__DefaultConnection`; aplikasi harus berhenti dengan pesan error yang jelas.

---

### `TSK-SEC-03`: Rate Limiting pada Login Endpoint
- **Tujuan:** Mencegah serangan brute-force dan credential stuffing.
- **Langkah Implementasi:**
  1. Tambahkan `services.AddRateLimiter(...)` dengan Fixed Window Policy (5 request/menit).
  2. Pasang atribut `.RequireRateLimiting("LoginPolicy")` pada endpoint `/api/auth/login`.
- **File Terdampak:**
  - [`src/ERP.Web/Program.cs`](file:///d:/erp_system/src/ERP.Web/Program.cs)
- **Dependensi:** `TSK-SEC-01`
- **Prioritas:** **P1 (Tinggi)** | **Effort:** 4 Jam | **Status:** Not Started
- **Metode Verifikasi:** Eksekusi 6 request login berturut-turut dalam 10 detik; request ke-6 harus menerima respons HTTP 429 Too Many Requests.

---

## 2. Fase 2: Performa Basis Data & Arsitektur (P1)

### `TSK-ARCH-01`: Migrasi `CurrentUserService` ke Blazor `AuthenticationStateProvider`
- **Tujuan:** Menjamin kestabilan resolusi konteks pengguna di seluruh circuit SignalR Blazor Server.
- **Langkah Implementasi:**
  1. Buat implementasi `BlazorCurrentUserService` yang menyuntikkan `AuthenticationStateProvider`.
  2. Daftarkan di container dependency injection dengan lifetime Scoped.
- **File Terdampak:**
  - [`src/ERP.Infrastructure/Services/CurrentUserService.cs`](file:///d:/erp_system/src/ERP.Infrastructure/Services/CurrentUserService.cs)
  - [`src/ERP.Infrastructure/DependencyInjection.cs`](file:///d:/erp_system/src/ERP.Infrastructure/DependencyInjection.cs)
- **Dependensi:** `TSK-SEC-01`
- **Prioritas:** **P1 (Tinggi)** | **Effort:** 6 Jam | **Status:** Not Started
- **Metode Verifikasi:** Putus koneksi jaringan sebentar (simulasi SignalR circuit reconnect); pastikan filter `CompanyId` dan nama pengguna tetap teresolusi dengan benar.

---

### `TSK-DB-01` s/d `TSK-DB-04`: Penerapan Indeks Kinerja Basis Data
- **Tujuan:** Mengeliminasi sequential table scans pada tabel volume transaksi tinggi.
- **Langkah Implementasi:**
  1. Tambahkan konfigurasi indeks pada `ApplicationDbContext.OnModelCreating`:
     - `journal_lines.account_id`
     - `stock_movements (warehouse_id, product_id, movement_date)`
     - `payment_allocations.invoice_id`
     - `sales_orders (company_id, status)` dan `purchase_orders (company_id, status)`
  2. Generate migrasi EF Core: `AddPerformanceAndIntegrityIndexes`.
  3. Terapkan migrasi ke database PostgreSQL.
- **File Terdampak:**
  - [`src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs`](file:///d:/erp_system/src/ERP.Infrastructure/Persistence/Context/ApplicationDbContext.cs)
- **Dependensi:** Tidak ada
- **Prioritas:** **P1 (Tinggi)** | **Effort:** 6 Jam | **Status:** Not Started
- **Metode Verifikasi:** Periksa output `EXPLAIN ANALYZE` di `psql` pada query kartu stok dan buku besar (harus menggunakan `Index Scan` bukan `Seq Scan`).

---

### `TSK-ARCH-03`: Integrasi Background Job Processing Engine (Hangfire)
- **Tujuan:** Memindahkan eksekusi pekerjaan periodik berat ke worker thread terisolasi.
- **Langkah Implementasi:**
  1. Tambahkan package NuGet `Hangfire.AspNetCore` dan `Hangfire.PostgreSql`.
  2. Konfigurasikan storage Hangfire pada skema `hangfire` di database PostgreSQL.
  3. Daftarkan recurring job untuk penutupan otomatis sesi kedaluwarsa.
- **File Terdampak:**
  - `src/ERP.Infrastructure/ERP.Infrastructure.csproj`
  - `src/ERP.Web/Program.cs`
- **Dependensi:** `TSK-DB-01`
- **Prioritas:** **P1 (Tinggi)** | **Effort:** 10 Jam | **Status:** Not Started
- **Metode Verifikasi:** Akses `/hangfire` dashboard dengan akun SuperAdmin; periksa recurring jobs aktif dan berhasil dieksekusi.

---

## 3. Fase 3: Kelengkapan Fungsional (P1 / P2)

### `TSK-FUNC-01` & `TSK-UI-01`: Modul Departemen (CQRS & Blazor View)
- **Tujuan:** Melengkapi fungsionalitas master departemen dan memperbaiki link 404 pada menu navigasi.
- **Langkah Implementasi:**
  1. Buat folder `src/ERP.Application/Features/Master/Departments/` berisi `DepartmentDto.cs`, `GetDepartmentsQuery.cs`, `CreateDepartmentCommand.cs`, `UpdateDepartmentCommand.cs`.
  2. Buat file `src/ERP.Web/Components/Pages/Master/DepartmentsPage.razor` dengan tabel MudBlazor dan dialog modal CRUD.
- **File Terdampak:**
  - `src/ERP.Application/Features/Master/Departments/*`
  - `src/ERP.Web/Components/Pages/Master/DepartmentsPage.razor`
- **Dependensi:** Tidak ada
- **Prioritas:** **P2 (Sedang)** | **Effort:** 10 Jam | **Status:** Not Started
- **Metode Verifikasi:** Klik menu "Departemen" di sidebar; halaman harus terbuka tanpa error 404 dan dapat melakukan operasi tambah, ubah, serta pencarian departemen.

---

### `TSK-SEC-05`: Perluasan Matriks Izin Granular (`View` Permissions)
- **Tujuan:** Mendukung prinsip *Least Privilege* bagi auditor dan staf read-only.
- **Langkah Implementasi:**
  1. Tambahkan konstanta izin baca di `Permissions.cs` (`sales.order.view`, `procurement.pr.view`, dll).
  2. Perbarui query handler untuk memeriksa izin `View` terkait.
  3. Perbarui `DatabaseSeeder.cs` untuk mendaftarkan izin baru ke database.
- **File Terdampak:**
  - [`src/ERP.Shared/Security/Permissions.cs`](file:///d:/erp_system/src/ERP.Shared/Security/Permissions.cs)
  - Query Handlers di `src/ERP.Application/Features/**/*`
  - [`src/ERP.Infrastructure/Persistence/Seeders/DatabaseSeeder.cs`](file:///d:/erp_system/src/ERP.Infrastructure/Persistence/Seeders/DatabaseSeeder.cs)
- **Dependensi:** Tidak ada
- **Prioritas:** **P2 (Sedang)** | **Effort:** 6 Jam | **Status:** Not Started
- **Metode Verifikasi:** Buat user dengan role Auditor (hanya memiliki izin `view`); pastikan user dapat melihat daftar pesanan tetapi ditolak saat mencoba membuat pesanan.

---

## 4. Fase 4: Antarmuka Pengguna & Pengujian Otomasi (P2)

### `TSK-UI-02`: Dynamic Active Branch Switcher pada Top Bar
- **Tujuan:** Memungkinkan pengguna multi-cabang beralih cabang operasional secara instan.
- **Langkah Implementasi:**
  1. Ubah indikator teks statis di `MainLayout.razor` menjadi `MudMenu`.
  2. Ambil daftar cabang yang diizinkan dari `CurrentUserService.AllowedBranchIds`.
  3. Simpan cabang aktif di sesi sirkuit Blazor dan perbarui filter query secara reaktif.
- **File Terdampak:**
  - [`src/ERP.Web/Components/Layout/MainLayout.razor`](file:///d:/erp_system/src/ERP.Web/Components/Layout/MainLayout.razor)
- **Dependensi:** `TSK-ARCH-01`
- **Prioritas:** **P2 (Sedang)** | **Effort:** 6 Jam | **Status:** Not Started
- **Metode Verifikasi:** Pilih Cabang Surabaya di menu top bar; data stok dan pesanan yang tampil di tabel harus otomatis menyesuaikan ke Cabang Surabaya.

---

### `TSK-TEST-01`: Pembuatan Test Suite Komponen UI (bUnit)
- **Tujuan:** Mencegah regresi visual dan interaksi pada komponen Blazor Razor.
- **Langkah Implementasi:**
  1. Buat proyek pengujian `tests/ERP.Web.Tests/ERP.Web.Tests.csproj` menggunakan library `bUnit`.
  2. Tulis test rendering untuk `MainLayout`, `NavMenu`, dan form dialog transaksi.
- **File Terdampak:**
  - `tests/ERP.Web.Tests/*`
- **Dependensi:** `TSK-UI-01`
- **Prioritas:** **P2 (Sedang)** | **Effort:** 12 Jam | **Status:** Not Started
- **Metode Verifikasi:** Jalankan `dotnet test tests/ERP.Web.Tests/` dan verifikasi seluruh pengujian render bUnit berhasil.
