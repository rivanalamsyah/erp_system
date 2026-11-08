# Enterprise Resource Planning (ERP) System

[![Architecture](https://img.shields.io/badge/Architecture-Modular%20Monolith%20%7C%20Clean-blue.svg)](docs/SOFTWARE_ARCHITECTURE.md)
[![Platform](https://img.shields.io/badge/.NET-10.0%20LTS-purple.svg)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/C%23-14.0-blue.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Frontend](https://img.shields.io/badge/Blazor-Interactive%20Server%20%2B%20MudBlazor%20v9-green.svg)](docs/UI_UX_GUIDELINES.md)
[![Database](https://img.shields.io/badge/PostgreSQL-18%20%28EF%20Core%2010%29-blue.svg)](docs/DATABASE_DESIGN.md)
[![Tests](https://img.shields.io/badge/Tests-38%20Passed%20%7C%200%20Failed-success.svg)](tests/)

General-purpose, scalable, enterprise-grade ERP system engineered with **.NET 10 LTS, C# 14, Blazor Web App (Interactive Server), MudBlazor v9, PostgreSQL 18, and EF Core 10** adopting **Clean Architecture** and **Modular Monolith** principles.

---

## 📌 Status Implementasi: **Fully Implemented & Verified (End-to-End Production Ready)**

Seluruh 9 modul inti ERP beserta fondasi arsitektur, keamanan PBAC, migrasi database PostgreSQL, dan automated unit/architecture tests telah diimplementasikan secara menyeluruh sesuai dokumentasi resmi di `/docs`.

### Ringkasan Pencapaian:
- **Database Engine:** PostgreSQL 18 (Daemon aktif pada `127.0.0.1:5433`).
- **Skema Database:** 56 Tabel nyata pada 8 Schema terisolasi (`identity`, `master`, `inventory`, `procurement`, `sales`, `finance`, `hr`, `audit`).
- **Business Rules Enforced:**
  - `BR-INV-001`: Pencegahan stok fisik negatif (*No-Negative Stock Invariant*).
  - `BR-SALES-001`: Validasi limit pagu kredit pelanggan (*Customer Credit Limit Enforcement*).
  - `BR-SALES-002`: Alokasi & reservasi stok tersedia saat konfirmasi SO (*Available Stock Allocation*).
  - `BR-SALES-003`: Validasi kuantitas pengiriman tidak boleh melebihi sisa pesanan (*Delivery Order Limit*).
  - `BR-ACC-001`: Keseimbangan buku besar entri ganda $\sum \text{Debit} = \sum \text{Credit}$ (*Zero Imbalance Invariant*).
  - `BR-ACC-002`: Penguncian transaksi pada periode fiskal yang telah ditutup (*Closed Fiscal Period Locking*).
  - `BR-ACC-003`: Alokasi pelunasan kas/bank tidak boleh melebihi sisa tagihan faktur (*Payment Allocation Cap*).
  - `BR-HR-001`: Validasi sisa kuota cuti tahunan karyawan (*Leave Quota Validation*).
  - `BR-HR-002`: Pencegahan gaji bersih negatif pada payroll (*Non-Negative Net Salary Invariant*).
- **Automated Tests:** **38 Passed, 0 Failed, 0 Skipped** (`ERP.Domain.Tests`, `ERP.Application.Tests`, `ERP.Architecture.Tests`).

---

## 🏢 Cakupan 9 Modul ERP Terintegrasi

| Modul | Rute URL | Fitur Utama |
|---|---|---|
| **Executive Dashboard** | `/` | KPI multi-modul real-time, status PostgreSQL, link cepat ke setiap modul operasional. |
| **Master Data** | `/master/*` | Perusahaan Multi-Entity, Kantor Cabang, Departemen, Katalog Produk (SKU, UoM, Kategori), Pelanggan (Credit Limit), Pemasok (Vendor). |
| **Tata Kelola & Keamanan** | `/security/*` | ASP.NET Core Identity, PBAC (Permission-Based Access Control), Data Scoping Company/Branch, Immutable Audit Trail. |
| **Persediaan (Inventory)** | `/inventory/*` | Multi-Warehouse, Saldo Stok Perpetual, Kartu Stok & Mutasi Masuk/Keluar, Transfer Antar-Gudang, Stock Opname & Penyesuaian. |
| **Pengadaan (Procurement)** | `/procurement/*` | Permintaan Pembelian (PR), Pesanan Pembelian (PO) dengan approval, Penerimaan Barang (GRN) perpetual, Tagihan Pemasok (Vendor Bills) & rekonsiliasi 3-way match. |
| **Penjualan (Sales)** | `/sales/*` | Surat Penawaran Harga (SQ), Pesanan Penjualan (SO) dengan reservasi stok otomatis, Surat Jalan & Pengiriman (DO), Faktur Penjualan (SI) & Piutang (AR). |
| **Keuangan & Akuntansi** | `/finance/*` | Bagan Akun (COA 5 Kategori: Aset, Kewajiban, Ekuitas, Pendapatan, Beban), Entri Jurnal Umum Seimbang, Kas & Bank, Pembayaran & Alokasi AR/AP, Penutupan Periode Fiskal Bulanan. |
| **SDM & Payroll (HR)** | `/hr/*` | Master Biodata Karyawan (NIK, KTP, Gaji Pokok), Presensi & Kehadiran (Check-In/Out & Keterlambatan), Manajemen Cuti & Kuota, Batch Payroll Bulanan (Gaji Pokok, Tunjangan, BPJS, PPh 21). |
| **Laporan & Dokumen** | `/reports/*` | Neraca Keuangan (Balance Sheet), Laba Rugi (Profit & Loss), Neraca Saldo (Trial Balance), Analitik Omzet Penjualan & Top Pelanggan/Produk, Valuasi Persediaan Stok Gudang. |

---

## 🛠️ Panduan Menjalankan Sistem

### 1. Prasyarat Environment
- .NET 10.0 SDK (`dotnet --version` >= 10.0.100)
- PostgreSQL 18
- PowerShell / Bash

### 2. Konfigurasi Database PostgreSQL
Database PostgreSQL berjalan secara default pada port `5433`:
```bash
# Menjalankan PostgreSQL Server Daemon
& "C:\Program Files\PostgreSQL\18\bin\postgres.exe" -D "d:\erp_system\data\pgdata" -p 5433
```
Konfigurasi koneksi tersimpan di `src/ERP.Web/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=127.0.0.1;Port=5433;Database=enterprise_erp;Username=erp_admin;Password=your_secure_password"
}
```

### 3. Menjalankan EF Core Migrations
```powershell
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"
$env:Path = "$env:USERPROFILE\.dotnet;$env:USERPROFILE\.dotnet\tools;$env:Path"

# Periksa status migrasi dan update ke database
dotnet ef database update -p src/ERP.Infrastructure -s src/ERP.Web
```

### 4. Menjalankan Automated Tests
```powershell
dotnet test
```
Hasil:
- `ERP.Domain.Tests`: 28 passed
- `ERP.Application.Tests`: 6 passed
- `ERP.Architecture.Tests`: 4 passed (Clean Architecture dependency rules validated)

### 5. Menjalankan Aplikasi Web Blazor
```powershell
dotnet run --project src/ERP.Web/ERP.Web.csproj
```
Buka browser pada: `http://localhost:5000` atau `https://localhost:5001`.

---

## 📂 Struktur Solusi (Clean Architecture Modular Monolith)

```
d:\erp_system/
├── src/
│   ├── ERP.Domain/           # Enterprise Business Rules, Entities, Enums, Value Objects
│   │   └── Entities/         # Master, Identity, Inventory, Procurement, Sales, Finance, Hr
│   ├── ERP.Application/      # Use Cases, CQRS (MediatR), DTOs, FluentValidation Behaviors
│   │   └── Features/         # Master, Inventory, Procurement, Sales, Finance, Hr, Reports
│   ├── ERP.Infrastructure/   # EF Core 10 DbContext, PostgreSQL Schemas, Npgsql, Migrations, Interceptors
│   ├── ERP.Shared/           # Shared Security Constants, Permissions, Results, Value Types
│   └── ERP.Web/              # Blazor Web App (Interactive Server), MudBlazor v9, Layouts, Pages
├── tests/
│   ├── ERP.Domain.Tests/     # Domain Invariant Tests (Stock, Journal Balancing, Credit Limit, Leave, Payroll)
│   ├── ERP.Application.Tests/# CQRS Validation & Business Logic Pipeline Tests
│   └── ERP.Architecture.Tests/# NetArchTest Architectural Dependency Rules
└── docs/                     # PRD, SDD, ERD, Data Dictionary, Workflows, Rules, API Specs
```

---
*Enterprise ERP Architecture & Engineering Group — 2026*
