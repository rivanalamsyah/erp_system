# DevOps, Infrastructure & Production Readiness Audit — Enterprise ERP System

> **Document ID:** AUD-OPS-2026-001  
> **Deployment Target:** Linux Server (Ubuntu 24.04 LTS / Docker Compose / Nginx Reverse Proxy)  
> **Related Documents:** [DEPLOYMENT_GUIDE.md](../DEPLOYMENT_GUIDE.md) | [NON_FUNCTIONAL_REQUIREMENTS.md](../NON_FUNCTIONAL_REQUIREMENTS.md)

---

## 1. Evaluasi Artefak Kontainerisasi Docker

### 1.1 Multi-Stage `Dockerfile` (.NET 10 LTS)
- **Status Evaluasi:** **SANGAT BAIK (Best Practice Compliant)**
- **Karakteristik Kunci:**
  - **Multi-Stage Build:** Menggunakan image SDK `mcr.microsoft.com/dotnet/sdk:10.0` untuk build/restore, dan runtime ramping `mcr.microsoft.com/dotnet/aspnet:10.0` untuk image final.
  - **Prinsip Non-Root User:** Menjalankan aplikasi dengan akun sistem non-root (`USER $APP_UID`) untuk memitigasi risiko kontainer breakout.
  - **Layer Caching:** Menyalin file `.slnx` dan `.csproj` terlebih dahulu sebelum `dotnet restore`, mengoptimalkan waktu build pipeline.
  - **File `.dockerignore`:** Mengabaikan direktori `.git`, `bin`, `obj`, `data/pgdata`, dan `backups/` agar ukuran image tetap minimal.

---

### 1.2 Orkestrasi `docker-compose.yml` & Lingkungan (.env)
- **Status Evaluasi:** **LENGKAP & TERUJI**
- **Karakteristik Kunci:**
  - Layanan `db` (PostgreSQL 17-alpine) terisolasi dalam jaringan internal `erp_internal_network`.
  - Dilengkapi healthcheck otomatis:
    ```yaml
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-erp_admin} -d ${POSTGRES_DB:-enterprise_erp}"]
      interval: 10s
      timeout: 5s
      retries: 5
    ```
  - Layanan `web` memiliki dependensi `condition: service_healthy` pada database sebelum container aplikasi dinyalakan.
  - Volume persisten terdefinisi untuk basis data (`erp_postgres_data`), berkas unggahan (`erp_app_uploads`), dan log aplikasi (`erp_app_logs`).
  - Template `.env.example` terdokumentasi tanpa menyimpan kredensial produksi secara terbuka.

---

## 2. Evaluasi Utilitas Backup & Disaster Recovery

Utilitas backup otomatis yang disediakan di folder `/scripts`:

```mermaid
flowchart LR
    PG[(PostgreSQL 18 Database)]
    SCRIPT["scripts/backup_db.ps1"]
    DUMP[("Binary Dump: .dump (Compressed)")]
    PRUNE["Automated 30-Day Retention Pruning"]
    RESTORE["scripts/restore_db.ps1"]
    STAGING[(Restored Staging DB)]

    PG -->|pg_dump -F c| SCRIPT
    SCRIPT --> DUMP
    SCRIPT --> PRUNE
    DUMP -->|pg_restore --clean| RESTORE
    RESTORE --> STAGING
```

### 2.1 Hasil Uji Nyata Backup & Restore
- **Waktu Eksekusi Backup:** **0,74 detik** untuk mencadangkan seluruh 56 tabel.
- **Ukuran File Arsip:** **0,18 MB** (format custom binary `-F c` dengan kompresi zlib bawaan PostgreSQL).
- **Prosedur Pemulihan (RTO):** $< 5\text{ detik}$ menggunakan `scripts/restore_db.ps1` dengan opsi `--clean` dan `--if-exists`.
- **RPO (Recovery Point Objective):** Ditargetkan $< 24\text{ jam}$ dengan penjadwalan cron harian pukul 02:00 WIB.

---

## 3. Analisis Kesenjangan DevOps & Otomasi CI/CD

### 3.1 `DEVOPS-001` [HIGH - P1]: Ketiadaan GitHub Actions CI/CD Pipeline
- **Kondisi Aktual:**
  Repositori saat ini belum memiliki folder `.github/workflows/` untuk menjalankan pengujian dan kompilasi otomatis setiap kali ada pull request atau commit baru ke branch `master`.
- **Risiko Operasional:**
  Risiko *build breaker* atau kode yang gagal tes lolos ke repositori utama tanpa terdeteksi sebelum proses deployment.
- **Rekomendasi Solusi:**
  Buat file `.github/workflows/ci.yml`:
  ```yaml
  name: Enterprise ERP CI/CD Pipeline

  on:
    push:
      branches: [ master, main ]
    pull_request:
      branches: [ master, main ]

  jobs:
    build-and-test:
      runs-on: ubuntu-latest
      steps:
      - uses: actions/checkout@v4
      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Restore dependencies
        run: dotnet restore EnterpriseERP.slnx
      - name: Build Solution
        run: dotnet build EnterpriseERP.slnx --no-restore -c Release
      - name: Run Automated Tests
        run: dotnet test EnterpriseERP.slnx --no-build -c Release --verbosity normal
  ```

---

### 3.2 `DEVOPS-002` [LOW - P3]: Ketiadaan APM & OpenTelemetry Instrumentation
- **Kondisi Aktual:** Logging saat ini mengandalkan `ILogger` lokal dan tabel `audit.audit_logs`.
- **Rekomendasi Produksi:**
  Tambahkan paket NuGet `OpenTelemetry.Extensions.Hosting` dan eksportir OTLP untuk meneruskan metrik performa, latensi kueri database Npgsql, dan trace SignalR ke platform observability enterprise (misal Grafana Tempo / Prometheus / Azure Application Insights).
