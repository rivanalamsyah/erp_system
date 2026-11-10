# Enterprise Resource Planning (ERP) System

[![Architecture](https://img.shields.io/badge/Architecture-Modular%20Monolith%20%7C%20Clean-blue.svg)](docs/SOFTWARE_ARCHITECTURE.md)
[![Platform](https://img.shields.io/badge/.NET-10.0%20LTS-purple.svg)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/C%23-14.0-blue.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Frontend](https://img.shields.io/badge/Blazor-Interactive%20Server%20%2B%20MudBlazor%20v9-green.svg)](docs/UI_UX_GUIDELINES.md)
[![Database](https://img.shields.io/badge/PostgreSQL-18%20%28EF%20Core%2010%29-blue.svg)](docs/DATABASE_DESIGN.md)
[![Tests](https://img.shields.io/badge/Tests-43%20Passed%20%7C%200%20Failed-success.svg)](tests/)
[![Security](https://img.shields.io/badge/Security-PBAC%20%7C%20CSRF%20Protected%20%7C%20Rate%20Limited-green.svg)](docs/audit/SECURITY_AUDIT.md)
[![CI/CD](https://img.shields.io/badge/CI%2FCD-GitHub%20Actions%20Ready-blue.svg)](.github/workflows/ci.yml)

General-purpose, scalable, enterprise-grade ERP system engineered with **.NET 10 LTS, C# 14, Blazor Web App (Interactive Server), MudBlazor v9, PostgreSQL 18, and EF Core 10** adopting **Clean Architecture** and **Modular Monolith** principles.

---

## 📌 Status Implementasi: **100% Production Ready & Verified**

Seluruh 9 modul inti ERP beserta fondasi arsitektur, keamanan enterprise (PBAC & CSRF Protection), indeks performa database PostgreSQL, dan automated test suite telah selesai diimplementasikan, diaudit, dan diverifikasi secara menyeluruh (*zero warnings, zero errors*).

### Ringkasan Pencapaian Utama:
- **Clean Architecture & Modular Monolith:** Pemisahan tegas antar-lapisan (*Core Domain, Application CQRS, Infrastructure, Shared, Web UI*) dengan kepatuhan SOLID 100%.
- **Database Relasional Skala Enterprise:** PostgreSQL 18 dengan **56 Tabel di 8 Skema Terisolasi** (`identity`, `master`, `inventory`, `procurement`, `sales`, `finance`, `hr`, `audit`).
- **PostgreSQL Performance Indexes:** Indeks komposit terpasang pada `journal_lines`, `stock_movements`, `sales_orders`, `purchase_orders`, dan `payment_allocations`.
- **Keamanan Siber Tingkat Enterprise:**
  - Token Antiforgery aktif pada alur otentikasi (Mitigasi CSRF OWASP A05).
  - Rate Limiter terpasang pada endpoint login (5 percobaan / menit).
  - Kredensial database fail-fast tanpa fallback hardcoded.
  - Granular Permission-Based Access Control (PBAC) dengan pipeline behavior server-side.
- **Blazor Interactive Server Circuit Resilience:**
  - Circuit retention dikonfigurasi 5 menit dengan kapasitas hingga 500 circuits.
  - SignalR buffer size 1 MB dengan keep-alive timeout 60 detik.
  - Dynamic branch switcher dan dynamic user profile menu di top bar.
- **Automated Tests Suite:** **43 Passed, 0 Failed, 0 Skipped** (`ERP.Domain.Tests`, `ERP.Application.Tests`, `ERP.IntegrationTests`, `ERP.Architecture.Tests`).
- **DevOps & Kontainerisasi:** Multi-stage `Dockerfile` (.NET 10 non-root user), `docker-compose.yml`, template `.env.example`, skrip backup otomatis (`scripts/backup_db.ps1`), dan skrip pemulihan database (`scripts/restore_db.ps1`).

---

## 🏢 Cakupan 9 Modul ERP Terintegrasi

| Modul | Rute URL | Fitur Utama & Invarian Bisnis yang Ditegakkan |
|---|---|---|
| **Executive Dashboard** | `/` | KPI multi-modul real-time MTD, status koneksi database PostgreSQL, dan tautan aksi cepat ke seluruh modul operasional. |
| **Master Data** | `/master/*` | Perusahaan Multi-Entity, Kantor Cabang, **Departemen Organisasi (`DepartmentsPage.razor`)**, Katalog Produk (SKU, UoM, Kategori), Pelanggan (Credit Limit), Pemasok (Vendor), Penomoran Dokumen Otomatis. |
| **Tata Kelola & Keamanan** | `/security/*` | ASP.NET Core Identity, PBAC (Permission-Based Access Control), **Profil Pengguna (`/security/profile`)**, **Ganti Password (`/security/change-password`)**, Role Matrix, dan Immutable Audit Trail JSONB. |
| **Persediaan (Inventory)** | `/inventory/*` | Multi-Warehouse, Saldo Stok Perpetual, Kartu Stok & Mutasi Masuk/Keluar (FIFO/Moving Average Cost), Transfer Antar-Gudang, Stock Opname & Penyesuaian. Menegakkan **`BR-INV-001`** (Pencegahan stok negatif). |
| **Pengadaan (Procurement)** | `/procurement/*` | Permintaan Pembelian (PR), Pesanan Pembelian (PO) dengan kalkulasi PPN 11% dan otorisasi, Penerimaan Barang (GRN) perpetual, Tagihan Pemasok (Vendor Bills), dan rekonsiliasi 3-way match. |
| **Penjualan (Sales)** | `/sales/*` | Surat Penawaran Harga (SQ), Pesanan Penjualan (SO), Surat Jalan (DO), Faktur Penjualan (SI) & Piutang (AR). Menegakkan **`BR-SALES-001`** (Limit kredit), **`BR-SALES-002`** (Reservasi stok), **`BR-SALES-003`** (Batas kirim). |
| **Keuangan & Akuntansi** | `/finance/*` | Bagan Akun (COA 5 Kategori), Kas & Bank, Pembayaran & Alokasi AR/AP, Penutupan Periode Fiskal Bulanan (**`BR-ACC-002`**). Menegakkan **`BR-ACC-001`** (Keseimbangan jurnal mutlak $\sum \text{Debit} == \sum \text{Kredit}$) dan **`BR-ACC-003`** (Batas alokasi tagihan). |
| **SDM & Payroll (HR)** | `/hr/*` | Master Biodata Karyawan (NIK, KTP, Status PKWT/Tetap), Presensi Kehadiran & Keterlambatan, Pengajuan & Otorisasi Cuti (**`BR-HR-001`** Kuota 12 hari), Batch Payroll Bulanan (**`BR-HR-002`** THP non-negatif, BPJS, PPh 21). |
| **Laporan & Dokumen** | `/reports/*` | Neraca Keuangan (Balance Sheet: Aset = Kewajiban + Ekuitas), Laba Rugi (Profit & Loss: Omset - HPP - Beban), Neraca Saldo (Trial Balance), Analitik Omzet Penjualan & Top Pelanggan/Produk, Valuasi Persediaan Stok Gudang. |
| **Pengaturan Sistem** | `/settings` | Konfigurasi tarif pajak global (PPN 11%), batas termin bayar, pratinjau sequence numbering, dan status pemeliharaan database. |

---

## 🛠️ Panduan Menjalankan Sistem

### 1. Prasyarat Lingkungan
- **SDK:** .NET 10.0 SDK (`dotnet --version` >= 10.0.100)
- **Database:** PostgreSQL 17 / 18
- **OS:** Windows / Linux (Ubuntu 22.04 / 24.04 LTS) / macOS
- **Container Runtime (Opsional):** Docker Engine 24+ & Docker Compose v2+

### 2. Konfigurasi Lingkungan
Salin file template `.env.example` ke `.env` (atau perbarui `appsettings.json`):
```powershell
cp .env.example .env
```
Pastikan string koneksi database terkonfigurasi dengan benar:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=127.0.0.1;Port=5433;Database=enterprise_erp;Username=erp_admin;Password=YourSecurePassword!;Maximum Pool Size=100;"
}
```

### 3. Eksekusi Migrasi Basis Data PostgreSQL
```powershell
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"
$env:Path = "$env:USERPROFILE\.dotnet;$env:USERPROFILE\.dotnet\tools;$env:Path"

dotnet ef database update --project src/ERP.Infrastructure --startup-project src/ERP.Web
```

### 4. Menjalankan Seluruh Pengujian Otomasi (Test Suite)
```powershell
dotnet test
```
Hasil:
- `ERP.Domain.Tests`: 28 Passed (Invarian domain & business rules)
- `ERP.Application.Tests`: 6 Passed (CQRS commands, queries, & validation)
- `ERP.IntegrationTests`: 5 Passed (Procure-to-Pay, Order-to-Cash, Zero Imbalance, HR Lifecycle)
- `ERP.Architecture.Tests`: 4 Passed (Clean architecture dependency boundaries)
- **Total:** **43 Passed, 0 Failed, 0 Skipped**

### 5. Menjalankan Aplikasi Web Blazor
```powershell
dotnet run --project src/ERP.Web/ERP.Web.csproj
```
Buka browser pada: `http://localhost:5000` atau `https://localhost:5001`.

**Kredensial Bawaan Administrator:**
- **Email:** `admin@enterprise.erp`
- **Kata Sandi:** `Admin@12345`

---

## 🐳 Panduan Deployment Kontainer Docker

Aplikasi telah dilengkapi kontainerisasi multi-stage siap produksi:

```bash
# 1. Salin konfigurasi environment
cp .env.example .env

# 2. Build dan nyalakan seluruh service kontainer
docker compose up -d --build

# 3. Periksa status kesehatan service
docker compose ps
```
Layanan web akan aktif pada port `5000` dengan database PostgreSQL pada port internal terisolasi `5432`.

---

## 💾 Utilitas Pencadangan & Pemulihan Basis Data (Backup & Disaster Recovery)

Tersedia skrip PowerShell otomatis di folder `/scripts`:

### 1. Pencadangan Terkompresi Otomatis (Backup):
```powershell
& "scripts/backup_db.ps1" -Hostname "127.0.0.1" -Port 5433 -DatabaseName "enterprise_erp" -Username "erp_admin" -RetentionDays 30
```
- Menghasilkan file dump biner terkompresi `enterprise_erp_backup_YYYYMMDD_HHMMSS.dump`.
- Secara otomatis membersihkan file cadangan yang berusia lebih dari 30 hari (*retention policy*).
- *Hasil Uji Nyata:* Mencadangkan 56 tabel database dalam **0,74 detik** (ukuran file ~0,18 MB).

### 2. Pemulihan Basis Data (Restore):
```powershell
& "scripts/restore_db.ps1" -Hostname "127.0.0.1" -Port 5433 -DatabaseName "enterprise_erp" -Username "erp_admin"
```
Secara otomatis mendeteksi file cadangan terbaru dan memulihkan seluruh struktur tabel dengan aman.

---

## 📑 Dokumentasi Audit & Rekayasa Lengkap

Hasil audit mendalam dan roadmap remediasi terdokumentasi di direktori [`/docs/audit/`](file:///d:/erp_system/docs/audit/):
- [`EXECUTIVE_SUMMARY.md`](docs/audit/EXECUTIVE_SUMMARY.md): Ringkasan eksekutif, skor kematangan, dan roadmap strategis.
- [`SYSTEM_AUDIT_REPORT.md`](docs/audit/SYSTEM_AUDIT_REPORT.md): Laporan audit lengkap 28 temuan berbasis bukti.
- [`MODULE_AUDIT_MATRIX.md`](docs/audit/MODULE_AUDIT_MATRIX.md): Matriks evaluasi detail seluruh 9 modul.
- [`ARCHITECTURE_AUDIT.md`](docs/audit/ARCHITECTURE_AUDIT.md): Evaluasi Clean Architecture, Modular Monolith, dan SOLID.
- [`DATABASE_AUDIT.md`](docs/audit/DATABASE_AUDIT.md): Audit 56 tabel, 8 skema, dan indeks performa.
- [`SECURITY_AUDIT.md`](docs/audit/SECURITY_AUDIT.md): Audit OWASP Top 10, PBAC permissions, dan CSRF.
- [`PERFORMANCE_AUDIT.md`](docs/audit/PERFORMANCE_AUDIT.md): Audit skalabilitas, kueri database, dan sirkuit SignalR.
- [`UI_UX_AUDIT.md`](docs/audit/UI_UX_AUDIT.md): Evaluasi MudBlazor design system dan responsivitas.
- [`TESTING_AUDIT.md`](docs/audit/TESTING_AUDIT.md): Analisis test coverage dan skenario pengujian.
- [`DEVOPS_AUDIT.md`](docs/audit/DEVOPS_AUDIT.md): Evaluasi kontainer Docker dan prosedur disaster recovery.
- [`TECHNICAL_DEBT_REGISTER.md`](docs/audit/TECHNICAL_DEBT_REGISTER.md): Daftar utang teknis dan estimasi penyelesaian.
- [`RISK_REGISTER.md`](docs/audit/RISK_REGISTER.md): Matriks risiko dan strategi mitigasi/rollback.
- [`IMPLEMENTATION_PLAN.md`](docs/audit/IMPLEMENTATION_PLAN.md): Rencana aksi remediasi 5 fase terstruktur.
- [`IMPLEMENTATION_ROADMAP.md`](docs/audit/IMPLEMENTATION_ROADMAP.md): Jadwal sprint, Quality Gates, dan Definition of Done.
- [`TASK_BREAKDOWN.md`](docs/audit/TASK_BREAKDOWN.md): Spesifikasi teknis setiap task remediasi.
- [`ACCEPTANCE_CRITERIA.md`](docs/audit/ACCEPTANCE_CRITERIA.md): Kriteria keberterimaan terukur (Given-When-Then).
- [`DOCUMENTATION_GAP_ANALYSIS.md`](docs/audit/DOCUMENTATION_GAP_ANALYSIS.md): Analisis keselarasan dokumentasi sistem.

---

## 🏛️ Lisensi & Hak Cipta
Hak Cipta © 2026 PT Enterprise Sinergi Nusantara. Seluruh hak cipta dilindungi undang-undang.
Sistem dilisensikan untuk penggunaan korporat terpadu.
