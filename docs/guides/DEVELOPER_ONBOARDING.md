# Developer Onboarding & Environment Setup Guide

> **Document Version:** 1.0.0  
> **Target Audience:** Software Engineers, QA Engineers, DevOps  
> **Related Documents:** [../CODING_STANDARDS.md](../CODING_STANDARDS.md) | [../PROJECT_STRUCTURE.md](../PROJECT_STRUCTURE.md) | [../DEPLOYMENT_GUIDE.md](../DEPLOYMENT_GUIDE.md)

---

## 1. Prasyarat Lingkungan Pengembangan (Development Prerequisites)

Sebelum memulai fase implementasi kode (setelah persetujuan resmi fase arsitektur), pastikan workstation pengembang memenuhi spesifikasi berikut:

| Perangkat Lunak | Versi yang Diwajibkan | Catatan Instalasi |
| :--- | :--- | :--- |
| **.NET SDK** | `.NET 10.0 LTS` | Verifikasi dengan perintah `dotnet --version` di terminal. |
| **C# Compiler** | `C# 14` | Didukung bawaan oleh .NET 10 SDK. |
| **Database Engine** | `PostgreSQL 16+` | Dapat diinstal langsung di host atau dijalankan via Docker. |
| **Container Engine** | `Docker Desktop / OrbStack` | Diperlukan untuk menjalankan database lokal dan Testcontainers. |
| **IDE Pilihan** | `Visual Studio 2026 / Rider / VS Code` | Pastikan ekstensi C# Dev Kit terpasang dengan baik. |
| **Global CLI Tools** | `dotnet-ef 10.x` | Install via: `dotnet tool install --global dotnet-ef --version 10.0.*` |

---

## 2. Menjalankan Lingkungan Lokal Berbasis Docker

Repositori ini telah menyediakan konfigurasi Docker Compose untuk menyalakan PostgreSQL dan Redis lokal:

```bash
# 1. Masuk ke root direktori proyek
cd d:/erp_system

# 2. Jalankan database PostgreSQL dan Redis lokal
docker compose up -d erp_postgres erp_redis

# 3. Verifikasi kontainer aktif dan siap menerima koneksi
docker ps
```

---

## 3. Konvensi Git Branching & Commit Messages

### Strategi Branching (GitFlow Simplified):
- `main`: Branch produksi yang dilindungi (*Protected Branch*). Hanya boleh diperbarui via Pull Request yang lulus seluruh check CI/CD.
- `develop`: Branch integrasi fitur aktif.
- `feature/[module]-[short-desc]`: Branch kerja pengembang (contoh: `feature/sales-quotation-crud`, `feature/inventory-stock-transfer`).
- `bugfix/[issue-number]-[short-desc]`: Branch perbaikan bug (contoh: `bugfix/fix-unbalanced-journal-calculation`).

### Format Commit Messages (Conventional Commits):
Gunakan konvensi standar:
- `feat(sales): add sales order confirmation handler with stock reservation`
- `fix(finance): prevent fiscal period close when unposted journals exist`
- `test(inventory): add unit tests for moving weighted average calculation`
- `docs(architecture): update database schema DDL for vendor bills`

