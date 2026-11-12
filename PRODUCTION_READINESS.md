# PRODUCTION READINESS ASSESSMENT — Evaluasi Kesiapan Produksi, DevOps, Observability & Disaster Recovery

> **Tanggal Audit:** 12 November 2026  
> **Target Lingkungan:** Containerized Production Deployment (Docker / Linux / PostgreSQL 17)

---

## 1. Skor Kesiapan Produksi (Production Readiness Scorecard)

| Dimensi Evaluasi | Bobot | Skor Aktual (0–100) | Status Kesiapan | Blocker Utama |
| :--- | :---: | :---: | :---: | :--- |
| **1. Authentication, Authorization & Multi-Tenancy** | 25% | **38 / 100** | 🔴 **Blocker** | `SEC-CRIT-01` (Claims tidak dihidrasi), `SEC-CRIT-02` (8 halaman crash), `SEC-CRIT-03` (13 halaman tanpa `[Authorize]`) |
| **2. Data Integrity & Concurrency Control** | 20% | **65 / 100** | 🔴 **Blocker** | `ARCH-CRIT-01` (`Scoped DbContext` di Blazor Circuit), `ARCH-CRIT-02` (`DocumentSequence` commit prematur), `DB-CRIT-01` (`StockBalance` tanpa `xmin`) |
| **3. Performance & Query Scalability** | 15% | **52 / 100** | 🔴 **Blocker** | `PERF-CRIT-01` (In-memory materialization pada laporan & dashboard), `PERF-HIGH-01` (N+1 queries di 8 handler) |
| **4. Business Workflow & Accounting Completeness** | 15% | **70 / 100** | ⚠️ **Perlu Perbaikan** | `ARCH-HIGH-01` (`FirstOrDefaultAsync` untuk Company/Branch), `ARCH-HIGH-02` (Sub-ledger belum auto-post ke GL & cek periode tutup) |
| **5. Observability, Logging & Health Checks** | 10% | **45 / 100** | 🔴 **Blocker** | `OPS-HIGH-01` (`Serilog` belum di-wire di `Program.cs`, tidak ada `/health` endpoint untuk container orchestrator) |
| **6. Containerization, CI/CD & Configuration Security** | 15% | **60 / 100** | ⚠️ **Perlu Perbaikan** | `OPS-HIGH-02` (`Database.MigrateAsync()` tidak dipanggil di `Production`), `SEC-MED-02` (`Include Error Detail=true` di `appsettings.json`) |
| **SKOR KESIAPAN PRODUKSI TERBOBOT** | **100%** | **54.6 / 100** | 🔴 **NO-GO (BELUM SIAP)** | **Wajib menyelesaikan minimal seluruh temuan Fase 1, Fase 2, dan Fase 3 sebelum Go-Live.** |

---

## 2. Temuan Infrastruktur, DevOps & Observability

---

### [OPS-HIGH-01] Paket `Serilog.AspNetCore` Terinstal Tetapi Belum Dikonfigurasi di `Program.cs` & Ketiadaan Endpoint `/health`
- **Severity:** `High`
- **Lokasi File & Baris:**
  - `src/ERP.Web/ERP.Web.csproj:14` (`<PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />`)
  - `src/ERP.Web/Program.cs:1-120`
  - `docker-compose.yml:43` (`- erp_logs:/app/logs`)
- **Akar Masalah & Dampak:**
  1. Meskipun `Serilog.AspNetCore` telah ditambahkan ke `ERP.Web.csproj` dan `docker-compose.yml` telah me-mount volume `/app/logs`, `Program.cs` sama sekali tidak memanggil `builder.Host.UseSerilog(...)` maupun `app.UseSerilogRequestLogging()`. Akibatnya, direktori `/app/logs` di container selalu kosong dan sistem tidak memiliki *structured JSON logging* untuk investigasi insiden produksi.
  2. `Program.cs` belum mendaftarkan `builder.Services.AddHealthChecks()` maupun `app.MapHealthChecks("/health")`, dan `docker-compose.yml:26-46` belum mendefinisikan `healthcheck` untuk service `web`. Load balancer / orchestrator tidak dapat mendeteksi apakah aplikasi dan koneksi PostgreSQL sehat.

---

### [OPS-HIGH-02] Container `Production` Tidak Menjalankan Migrasi Skema Database Saat Startup
- **Severity:** `High`
- **Lokasi File & Baris:**
  - `src/ERP.Web/Program.cs:64-75`
  - `Dockerfile:35` (`ENV ASPNETCORE_ENVIRONMENT=Production`)
  - `docker-compose.yml:38` (`ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Production}`)
- **Akar Masalah & Dampak:**
  - Di `Program.cs:64`, pemanggilan `await DatabaseSeeder.SeedAsync(app.Services)` dibungkus di dalam `if (app.Environment.IsDevelopment())`.
  - Karena `Dockerfile` dan `docker-compose.yml` berjalan dengan `ASPNETCORE_ENVIRONMENT=Production`, saat container pertama kali dinyalakan pada PostgreSQL baru, `context.Database.MigrateAsync()` tidak pernah dipanggil. Seluruh tabel (`identity`, `master`, `inventory`, dll.) tidak terbentuk dan aplikasi gagal melayani login.
- **Rekomendasi Perbaikan Teknis:**
  - Pisahkan eksekusi migrasi skema (`await context.Database.MigrateAsync()`) dan seeding struktur dasar wajib (Roles, RolePermissions, Company & SuperAdmin awal jika database kosong) agar berjalan secara idempoten baik di `Development` maupun `Production` (dikontrol melalui konfigurasi `Database:AutoMigrateOnStartup = true`), sedangkan data transaksi dummy tetap dibatasi hanya untuk `Development` atau flag `Database:SeedDemoData`.

---

### [OPS-MED-01] Konfigurasi Keamanan Cookie, Forwarded Headers & Data Protection Keys di Container
- **Severity:** `Medium`
- **Lokasi File & Baris:**
  - `src/ERP.Infrastructure/DependencyInjection.cs:48-51`
  - `src/ERP.Web/Program.cs:62-86`
  - `docker-compose.yml:41-44`
- **Akar Masalah & Dampak:**
  1. **Data Protection Keys Tidak Dipersistensi di Volume Docker:** Saat container `erp-web` di-restart atau di-redeploy, kunci enkripsi *Data Protection* ASP.NET Core hilang karena disimpan di dalam filesystem container sementara (`~/.aspnet/DataProtection-Keys`). Akibatnya, seluruh pengguna yang sedang login langsung ter-logout dan antiforgery token gagal divalidasi setiap kali container restart.
  2. **Forwarded Headers untuk Reverse Proxy (Nginx / Cloudflare / Traefik):** `Program.cs` belum mengonfigurasi `UseForwardedHeaders()`, sehingga jika aplikasi ditempatkan di balik Reverse Proxy, `HttpContext.Connection.RemoteIpAddress` akan selalu terbaca sebagai IP internal proxy (merusak pencatatan IP Audit Log dan Rate Limiting per IP).

---

## 3. Checklist Kesiapan Produksi (Go-Live Gate Checklist)

| Kategori | Item Pemeriksaan | Status Saat Ini | Target Setelah Remediasi |
| :--- | :--- | :---: | :---: |
| **Security** | Hidrasi klaim `Permission`, `CompanyId`, `BranchId` via `IUserClaimsPrincipalFactory` | ❌ Gagal | ✅ Lulus |
| **Security** | Registrasi `PermissionAuthorizationPolicyProvider` untuk seluruh 38 Permission | ❌ Gagal | ✅ Lulus |
| **Security** | Proteksi `@attribute [Authorize]` menyeluruh di seluruh halaman UI | ❌ Gagal (13 halaman terbuka) | ✅ Lulus |
| **Security** | Atribut `[RequirePermission]` pada seluruh Command & Query MediatR | ❌ Gagal (18 request terbuka) | ✅ Lulus |
| **Security** | Partisi Rate Limiter Login per alamat IP klien | ❌ Gagal (Global bucket) | ✅ Lulus |
| **Security** | Proteksi Anti-CSRF pada `/api/auth/logout` | ❌ Gagal | ✅ Lulus |
| **Security** | Penghapusan `Include Error Detail=true` dan kredensial dari `appsettings.json` & `Login.razor` | ❌ Gagal | ✅ Lulus |
| **Database** | Concurrency token `xmin` pada `StockBalance` & `DocumentSequence` | ❌ Gagal | ✅ Lulus |
| **Database** | Global Query Filter (`HasQueryFilter`) pada `FiscalPeriod`, `AttendanceRecord`, `LeaveRequest`, `AuditLog` | ❌ Gagal | ✅ Lulus |
| **Database** | Indeks komposit pada `audit_logs`, `journal_entries`, `sales_invoices`, `vendor_bills` | ❌ Gagal | ✅ Lulus |
| **Architecture** | Transaksi atomik pada `DocumentSequenceService` (tanpa `SaveChangesAsync` prematur) | ❌ Gagal | ✅ Lulus |
| **Architecture** | Isolasi `DbContext` di Blazor Circuit & penghapusan query langsung di `.razor` | ❌ Gagal | ✅ Lulus |
| **Business** | Otomasi jurnal GL dari Sub-Ledger (GRN, Delivery, Invoice, Bill, Payment, Payroll) | ❌ Gagal | ✅ Lulus |
| **Business** | Validasi penolakan posting pada `FiscalPeriod` yang sudah `IsClosed` (`BR-ACC-002`) | ❌ Gagal | ✅ Lulus |
| **Performance** | Agregasi SQL `GROUP BY` pada `GetFinancialReportsQuery`, `GetSalesAnalyticsQuery`, & `Home.razor` | ❌ Gagal | ✅ Lulus |
| **Performance** | Eliminasi query N+1 pada 8 Command Handler transaksi & `UsersPage.razor` | ❌ Gagal | ✅ Lulus |
| **Operations** | Konfigurasi `Serilog` (Console + Rolling File di `/app/logs`) & endpoint `/health` | ❌ Gagal | ✅ Lulus |
| **Operations** | Persistensi Data Protection Keys & eksekusi migrasi otomatis terkontrol di Docker | ❌ Gagal | ✅ Lulus |

---

## 4. Area yang Membutuhkan Konfirmasi / Investigasi Lanjutan Bersama Stakeholder Bisnis

Meskipun seluruh kode sumber di repositori telah diaudit 100% secara faktual, terdapat beberapa keputusan bisnis yang perlu dikonfirmasi sebelum atau selama tahap implementasi Fase 4:
1. **Pemetaan Default Akun Buku Besar (COA Mapping) untuk Auto-Jurnal Sub-Ledger:**
   - Kode akun COA mana yang menjadi default untuk *Persediaan Barang Dagang* (`1104`?), *GR/IR Clearing (Barang Diterima Belum Difakturkan)*, *Piutang Usaha* (`1103`?), *Hutang Usaha* (`2101`?), *PPN Masukan / PPN Keluaran*, *Pendapatan Penjualan* (`4101`?), *HPP* (`5101`?), dan *Beban Gaji / Hutang PPh 21* (`6101`?)?
2. **Kebijakan Isolasi Data pada Role `SUPER_ADMIN` vs Tenant Admin:**
   - Apakah `SUPER_ADMIN` memang dirancang untuk dapat melihat lintas perusahaan (`CompanyId == Guid.Empty` atau *tenant switcher*), sementara seluruh role lain wajib terkunci ketat pada satu `CompanyId`?
3. **Metode Perhitungan PPh 21 pada Modul Payroll (`BR-HR-002`):**
   - Saat ini `CalculatePayrollCommand.cs:54` menggunakan rumus disederhanakan (`5%` dari penghasilan bruto di atas PTKP Rp 4.500.000). Apakah untuk rilis produksi pertama rumus ini dipertahankan atau perlu disesuaikan dengan tabel Tarif Efektif Rata-Rata (TER PP 58/2023)?

