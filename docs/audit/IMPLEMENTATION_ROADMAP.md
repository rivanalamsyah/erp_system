# Implementation Roadmap & Quality Gates — Enterprise ERP Remediation

> **Document ID:** AUD-RDM-2026-001  
> **Timeline Horizon:** 4 Sprint (3.5 Minggu Kalender)  
> **Related Documents:** [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md) | [TASK_BREAKDOWN.md](./TASK_BREAKDOWN.md)

---

## 1. Jadwal Roadmap Bertahap (Sprint Execution Roadmap)

```mermaid
gantt
    title Roadmap Pelaksanaan Remediasi Enterprise ERP
    dateFormat  YYYY-MM-DD
    section Sprint 1: Security & DB (W1)
    SEC-01 Antiforgery Login Remediation      :crit, active, 2026-11-11, 2d
    SEC-02 Remove Hardcoded Connection String :crit, 2026-11-11, 1d
    ARCH-01 CurrentUserService SignalR Fix    :2026-11-13, 2d
    DB-01 s/d DB-04 Performance Indexes       :2026-11-14, 2d
    DEVOPS-01 GitHub Actions CI Setup         :2026-11-15, 1d
    section Sprint 2: Core Master & UI (W2)
    FUNC-01 & UI-01 Departments Module        :2026-11-18, 3d
    UI-02 Dynamic Branch Switcher Topbar      :2026-11-20, 2d
    UI-03 Profile & Password Management       :2026-11-21, 2d
    SEC-05 Granular Permission Extension      :2026-11-22, 2d
    section Sprint 3: Jobs & Export (W3)
    ARCH-03 Hangfire Job Processing Engine    :2026-11-25, 3d
    FUNC-02 In-App Notification System        :2026-11-27, 3d
    FUNC-03 Excel/PDF Document Exporter       :2026-11-29, 2d
    ARCH-02 CQRS Lookup Refactoring           :2026-11-30, 2d
    section Sprint 4: Hardening & Go-Live (W4)
    TEST-01 bUnit Razor Component Tests       :2026-12-02, 3d
    TEST-02 Concurrency Stress Test Suite     :2026-12-04, 2d
    DEVOPS-02 & 03 Nginx SSL & DR Drill       :2026-12-05, 2d
    UAT-01 User Acceptance Testing            :crit, 2026-12-08, 4d
```

---

## 2. Definisi Selesai (Definition of Done — DoD)

Sebuah tugas atau modul dinyatakan **SELESAI (DONE)** jika dan hanya jika memenuhi seluruh parameter berikut:

1. **Kompilasi Bersih (.NET 10):** Proyek terkompilasi dengan **0 Warning(s) dan 0 Error(s)**.
2. **Kepatuhan MudBlazor v9:** Tidak ada atribut usang atau warning analyzer `MUD0002`.
3. **Automated Unit & Integration Test:** Minimal 1 pengujian otomasi baru dibuat untuk use case terkait dan seluruh pengujian lama tetap lulus 100% (*zero regression*).
4. **Server-Side Security Enforcement:** Izin akses tervalidasi via `RequirePermissionAttribute` pada pipeline MediatR, bukan hanya disembunyikan di visual UI.
5. **Integritas Transaksi Basis Data:** Operasi mutasi data multi-tabel dibungkus dalam EF Core execution strategy / transaksi eksplisit.
6. **Code Review & Static Analysis:** Kepatuhan terhadap pedoman arsitektur Clean Architecture dan prinsip SOLID.
7. **Dokumentasi Terbarui:** File panduan, changelog, dan referensi silang dokumentasi diperbarui secara sinkron.

---

## 3. Gerbang Kualitas Antar-Fase (Quality Gates)

| Quality Gate | Fase Target | Kriteria Kelulusan Wajib (Entry Criteria) | Aksi Jika Gagal |
| :--- | :---: | :--- | :--- |
| **Gate 1: Keamanan Fondasi** | Akhir Sprint 1 | Proteksi CSRF aktif pada login, zero hardcoded password, seluruh indeks database diterapkan via migrasi. | Sprint 2 ditunda; fokus menyelesaikan perbaikan keamanan P0. |
| **Gate 2: Kelengkapan Navigasi** | Akhir Sprint 2 | Zero 404 broken link di seluruh sidebar dan topbar, modul departemen berfungsi penuh. | Larang pembukaan akses UAT ke pengguna eksternal. |
| **Gate 3: Ketahanan Beban** | Akhir Sprint 3 | Background job berjalan terisolasi, memory leak circuit SignalR tertangani, laporan ditarik $< 1.5$ detik. | Optimasi query SQL dan periksa change tracker EF Core. |
| **Gate 4: Persetujuan Go-Live** | Akhir Sprint 4 | 100% skenario UAT lolos verifikasi stakeholder bisnis, disaster recovery drill terbukti $< 2$ jam. | Rilis produksi dibatalkan; jadwalkan remedi tambahan. |

---

## 4. Persyaratan Pengujian Regresi (Regression Testing Requirements)

Setiap merge kode ke branch utama wajib memicu eksekusi pengujian regresi:
1. **Regresi Aturan Finansial:** Verifikasi invarian keseimbangan jurnal mutlak ($\sum \text{Debit} == \sum \text{Kredit}$) dan pencegahan mutasi periode tertutup.
2. **Regresi Alokasi Persediaan:** Verifikasi saldo stok gudang tidak pernah menjadi negatif pada kondisi apapun.
3. **Regresi Multi-Tenant:** Verifikasi kueri tidak membocorkan data antar perusahaan/cabang.

