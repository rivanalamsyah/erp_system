# Documentation Gap Analysis & Alignment Plan

> **Document ID:** AUD-DOC-2026-001  
> **Related Documents:** [SYSTEM_AUDIT_REPORT.md](./SYSTEM_AUDIT_REPORT.md) | [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)

---

## 1. Analisis Kesenjangan Dokumentasi vs Implementasi Aktual

Audit membandingkan dokumen yang ada di direktori `/docs` dengan kode sumber dan skema basis data aktual untuk mengidentifikasi dokumen yang perlu disinkronkan:

| Dokumen di `/docs` | Kondisi Dokumentasi Saat Ini | Kondisi Aktual pada Source Code & Database | Status Kesenjangan | Tindakan Rekomendasi |
| :--- | :--- | :--- | :---: | :--- |
| **`SYSTEM_DESIGN.md`** | Menyebutkan Redis Cache dan Hangfire sebagai komponen aktif. | Redis dan Hangfire belum diintegrasikan ke proyek C#. | **Ada Kesenjangan** | Beri catatan status *"Direncanakan pada Fase 2/3"* agar tidak membingungkan tim audit. |
| **`DATABASE_SCHEMA.md`** | Mendefinisikan seluruh 56 tabel di 8 skema secara komprehensif. | 56 tabel telah dibuat di PostgreSQL dengan struktur identik. | **100% SINKRON** | Tambahkan dokumentasi indeks komposit baru yang ditambahkan pada migrasi performa. |
| **`MODULE_SPECIFICATIONS.md`**| Menyebutkan sub-menu Master Departemen. | UI Departemen belum terimplementasi (`DepartmentsPage.razor` 404).| **Ada Kesenjangan** | Tandai status *"Under Remediation - Sprint 2"*. |
| **`USER_ROLES_AND_PERMISSIONS.md`**| Mendefinisikan matriks izin RBAC/PBAC. | Beberapa modul menggunakan izin `create` untuk aksi read-only. | **Ada Kesenjangan** | Perbarui matriks untuk memasukkan izin `view` granular (`sales.order.view`, dll). |
| **`DEPLOYMENT_GUIDE.md`** | Berisi panduan Linux Bash dan skrip PowerShell Windows. | Telah disinkronkan dengan `scripts/backup_db.ps1` dan `restore_db.ps1`. | **100% SINKRON** | Dokumentasi mutakhir dan valid. |
| **`SECURITY_DESIGN.md`** | Mensyaratkan CSRF protection di seluruh form dan password complexity. | Antiforgery dinonaktifkan di login (`.DisableAntiforgery()`). | **Kesenjangan Kritis**| Jadikan acuan untuk remediasi `TSK-SEC-01`. |

---

## 2. Rencana Pembaruan Dokumentasi (Documentation Maintenance Action Plan)

Agar dokumentasi di `/docs` tetap menjadi *Single Source of Truth* yang terpercaya:

1. **Sinkronisasi Matriks Izin (`USER_ROLES_AND_PERMISSIONS.md`):** Perbarui tabel peran dan hak akses segera setelah `TSK-SEC-05` selesai diimplementasikan.
2. **Pembaruan Diagram Arsitektur (`SOFTWARE_ARCHITECTURE.md`):** Perbarui diagram alur autentikasi Blazor Server setelah migrasi ke `AuthenticationStateProvider`.
3. **Penyempurnaan Kamus Data (`DATA_DICTIONARY.md`):** Tambahkan catatan indeks komposit baru pada tabel `journal_lines` dan `stock_movements`.
4. **Pemeliharaan Changelog (`CHANGELOG.md`):** Catat setiap rilis remediasi audit dengan penomoran versi semantik teratur (`v1.0.1-security`, `v1.1.0-features`).

