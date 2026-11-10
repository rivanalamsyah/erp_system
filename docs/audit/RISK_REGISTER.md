# Risk Register & Mitigation Strategy — Enterprise ERP System

> **Document ID:** AUD-RSK-2026-001  
> **Related Documents:** [SYSTEM_AUDIT_REPORT.md](./SYSTEM_AUDIT_REPORT.md) | [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)

---

## 1. Matriks Penilaian Risiko (Risk Scoring Matrix)

Risiko dievaluasi berdasarkan kombinasi **Kemungkinan Terjadinya (*Likelihood*)** dan **Dampak Dampak Bisnis/Teknis (*Impact*)**:

```mermaid
quadrantChart
    title Risk Priority Matrix
    x-axis Low Likelihood --> High Likelihood
    y-axis Low Impact --> High Impact
    quadrant-1 Immediate Mitigation (P0)
    quadrant-2 High Risk / Strict Controls (P1)
    quadrant-3 Monitor & Low Priority (P3)
    quadrant-4 Medium Risk / Planned Action (P2)
    "RSK-001: CSRF on Auth": [0.75, 0.90]
    "RSK-002: Hardcoded Secrets": [0.60, 0.85]
    "RSK-003: SignalR Disconnect State": [0.70, 0.75]
    "RSK-004: Missing DB Indexes": [0.85, 0.70]
    "RSK-005: Broken UI Links": [0.90, 0.45]
    "RSK-006: Background Job Freeze": [0.55, 0.70]
    "RSK-007: Concurrency Overwrite": [0.40, 0.80]
```

---

## 2. Register Risiko Lengkap

| ID Risiko | Kategori | Deskripsi Risiko | Kemungkinan | Dampak | Tingkat Risiko | Strategi Mitigasi Terencana | Rencana Kontinjensi & Rollback |
| :--- | :--- | :--- | :---: | :---: | :---: | :--- | :--- |
| **`RSK-001`** | Keamanan | Eksploitasi CSRF pada endpoint login akibat antiforgery disabled. | Tinggi | Kritis | **P0 (Kritis)** | Aktifkan middleware Antiforgery dan sertakan token pada form Razor. | Jika login bermasalah, periksa konfigurasi cookie SameSite dan CORS. |
| **`RSK-002`** | Keamanan | Kebocoran kredensial database akibat fallback plaintext di source code. | Sedang | Kritis | **P1 (Tinggi)** | Terapkan fail-fast exception jika connection string di env tidak ditemukan. | Gunakan environment variable terenkripsi di server produksi. |
| **`RSK-003`** | Arsitektur| Circuit SignalR kehilangan konteks pengguna saat koneksi terputus. | Tinggi | Tinggi | **P1 (Tinggi)** | Ganti `IHttpContextAccessor` dengan `AuthenticationStateProvider`. | Sediakan circuit reconnection dialog dengan auto-refresh token. |
| **`RSK-004`** | Performa | Latensi query jurnal dan mutasi stok melonjak saat volume transaksi tinggi. | Tinggi | Tinggi | **P1 (Tinggi)** | Tambahkan indeks komposit pada `journal_lines` dan `stock_movements`. | Rollback migrasi database via `dotnet ef database update` jika indeks gagal. |
| **`RSK-005`** | Usability | Pengguna menemui error 404 pada menu Departemen atau Profil Akun. | Sangat Tinggi | Rendah | **P2 (Sedang)** | Selesaikan `DepartmentsPage.razor` dan halaman profil akun. | Sembunyikan menu dari `NavMenu.razor` sementara sebelum halaman siap. |
| **`RSK-006`** | Stabilitas | Thread UI Blazor Server terblokir saat memproses kalkulasi payroll/valuasi massal. | Sedang | Tinggi | **P1 (Tinggi)** | Pisahkan proses periodik ke background job engine (Hangfire). | Batasi eksekusi kalkulasi massal hanya pada jam non-operasional. |
| **`RSK-007`** | Integritas | Penolakan pesanan penjualan akibat saldo persediaan berubah bersamaan. | Rendah | Tinggi | **P2 (Sedang)** | Pertahankan token konkurensi `xmin` dan tangani concurrency exception di UI. | Berikan notifikasi real-time ke pengguna untuk memuat ulang sisa stok. |
| **`RSK-008`** | Kepatuhan | PPh 21 atau perhitungan pajak PPN tidak sesuai dengan regulasi terbaru. | Rendah | Kritis | **P1 (Tinggi)** | Buat konfigurasi tarif pajak dinamis di tabel `SettingsPage` (PPN 11%, TER PPh 21). | Dukung penyesuaian tarif tanpa kompilasi ulang kode aplikasi. |
| **`RSK-009`** | Operasional| Kehilangan data akibat kegagalan server tanpa backup teruji. | Rendah | Bencana | **P1 (Tinggi)** | Jalankan skrip `backup_db.ps1` harian dan lakukan uji restore berkala. | Jalankan `restore_db.ps1` untuk memulihkan database dari dump terbaru. |

---

## 3. Protokol Rollback untuk Perubahan Berisiko Tinggi (Rollback Protocols)

### 3.1 Rollback Migrasi Basis Data EF Core
Jika migrasi indeks baru menimbulkan locking atau kendala performa:
```powershell
# Kembalikan skema ke migrasi stabil sebelumnya:
dotnet ef database update 20261108085126_AddHrModule --project src/ERP.Infrastructure --startup-project src/ERP.Web
```

### 3.2 Rollback Deployment Kontainer Docker
Jika container rilis baru mengalami kegagalan circuit:
```bash
# Kembalikan ke image build stabil sebelumnya:
docker compose down
docker compose up -d --no-build
```
