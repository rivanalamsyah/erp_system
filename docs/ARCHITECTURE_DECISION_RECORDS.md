# Architectural Decision Records (ADRs) — Enterprise ERP System

> **Document Version:** 1.0.0  
> **Status:** All Records Approved (Active Baseline)  
> **Scope:** Core Platform & Enterprise Architecture  
> **Related Documents:** [SYSTEM_DESIGN.md](./SYSTEM_DESIGN.md) | [SOFTWARE_ARCHITECTURE.md](./SOFTWARE_ARCHITECTURE.md) | [DATABASE_DESIGN.md](./DATABASE_DESIGN.md)

---

## Ringkasan Matriks Keputusan Arsitektur (ADR Index)

| ID | Judul Keputusan | Status | Dampak Arsitektural |
| :--- | :--- | :---: | :--- |
| **ADR-001** | Pemilihan .NET 10 LTS & C# 14 sebagai Platform Utama | Disetujui | Menjamin stabilitas performa enterprise & masa dukungan LTS jangka panjang hingga akhir 2028. |
| **ADR-002** | Pengadopsian Modular Monolith vs Microservices | Disetujui | Mengeliminasi latensi jaringan terdistribusi dan menyederhanakan transaksi ACID lintas modul. |
| **ADR-003** | Penggunaan Blazor Web App Interactive Server | Disetujui | Seluruh logika & state tetap di server, zero client token exposure, performa tinggi untuk ERP internal. |
| **ADR-004** | Pemilihan MudBlazor sebagai Design System & UI Library | Disetujui | Standarisasi Material Design, komponen data-table server-side yang matang, produktivitas tinggi. |
| **ADR-005** | PostgreSQL Multi-Domain Schemas dalam Satu Database | Disetujui | Partisi logis tabel bisnis (*identity, master, hr, inventory, dll*) dengan foreign key antar schema tetap utuh. |
| **ADR-006** | Entity Framework Core 10 & Npgsql sebagai ORM Utama | Disetujui | Dukungan native PostgreSQL types (UUIDv7, JSONB, tsvector, xmin concurrency), LINQ typesafe. |
| **ADR-007** | ASP.NET Core Identity dengan Permission-Based Access Control | Disetujui | Granularitas hak akses hingga level aksi (`sales.order.approve`), bukan sekadar pembagian role kasar. |
| **ADR-008** | Strategi Multi-Company & Multi-Branch berbasis Kolom Diskriminator | Disetujui | Satu database terpadu dengan penyaringan query global (*EF Global Query Filter*) untuk kemudahan konsolidasi. |
| **ADR-009** | Mekanisme Penomoran Dokumen Otomatis Terformat | Disetujui | Penomoran urut tanpa celah (*gapless sequence*) yang aman dari konflik konkurensi antar cabang. |
| **ADR-010** | Strategi Audit Trail Menggunakan EF Core SaveChanges Interceptor | Disetujui | Otomasi perekaman riwayat perubahan nilai kolom dalam format JSONB tanpa polusi kode handler bisnis. |
| **ADR-011** | Penegakan Keseimbangan Jurnal Akuntansi Double-Entry | Disetujui | Validasi ketat $\sum Debit = \sum Credit$ di domain invariant dan database check constraint. |
| **ADR-012** | Pemrosesan Latar Belakang Menggunakan Hangfire | Disetujui | Penjadwalan recurring job dan antrean task berat terdistribusi dengan dashboard pemantauan visual. |

---

## ADR-001: Pemilihan .NET 10 LTS & C# 14 sebagai Platform Utama

### Konteks Bisnis & Teknis
Sistem ERP membutuhkan framework yang memiliki kestabilan jangka panjang, performa I/O tinggi, keamanan memori teruji, serta ekosistem library enterprise yang matang. Pada kalender rilis Microsoft, .NET 10 merupakan versi Long-Term Support (LTS) aktif dengan dukungan resmi 3 tahun.

### Keputusan
Mengadopsi **.NET 10 LTS** dengan bahasa pemrograman **C# 14** untuk seluruh komponen backend, application service, dan web server.

### Konsekuensi & Mitigasi
- **Positif:** Dukungan runtime berkinerja tinggi, fitur bahasa C# 14 mutakhir, kompatibilitas terjamin dengan EF Core 10 dan Npgsql 10, serta pemeliharaan keamanan berkala dari vendor.
- **Negatif:** Tim pengembang wajib memastikan tooling lingkungan (Visual Studio 2026 / .NET 10 SDK) terinstal seragam.

---

## ADR-002: Pengadopsian Modular Monolith vs Microservices

### Konteks Bisnis & Teknis
ERP korporat melibatkan alur bisnis transaksional yang kompleks dan saling bergantung (misal: Konfirmasi Order $\rightarrow$ Reservasi Stok $\rightarrow$ Pengiriman Barang $\rightarrow$ Pembentukan Jurnal Akuntansi). Arsitektur Microservices mengharuskan implementasi Saga pattern, Distributed Transaction coordinator, dan eventual consistency yang rentan menghasilkan inkonsistensi saldo persediaan atau ketidakseimbangan jurnal jika terjadi gangguan jaringan internal.

### Keputusan
Mengadopsi pola **Modular Monolith** dengan prinsip Clean Architecture. Seluruh modul berada dalam satu solution dan berjalan dalam satu proses aplikasi, namun batas modularitas diisolasi secara tegas melalui namespace, DTO, dan in-process event.

### Konsekuensi & Mitigasi
- **Positif:** Mendukung transaksi ACID atomik di level basis data; debugging dan pengujian integrasi jauh lebih sederhana; biaya infrastruktur server lebih terjangkau.
- **Negatif:** Risiko batas modularitas dilanggar (*spaghetti dependencies*) jika pengembang tidak disiplin.  
  *Mitigasi:* Menggunakan arsitektur test (*NetArchTest*) dalam pipeline CI/CD untuk menggagalkan build jika ada dependensi siklis antar modul.

---

## ADR-003: Penggunaan Blazor Web App Interactive Server

### Konteks Bisnis & Teknis
Pengembangan ERP membutuhkan kecepatan iterasi tinggi, form input yang padat data, dan kontrol keamanan yang ketat. Membangun aplikasi menggunakan SPA terpisah (React/Vue/Angular) mengharuskan pemeliharaan dua codebase berbeda, serialisasi DTO REST API ganda, serta risiko keamanan penyimpanan kredensial/token di browser client.

### Keputusan
Memilih **Blazor Web App dengan Interactive Server Mode**. Seluruh kode UI dan interaksi event berjalan di server via koneksi WebSocket SignalR persisten.

### Konsekuensi & Mitigasi
- **Positif:** Single codebase C#; kode bisnis tidak pernah terdistribusi ke client (keamanan 100% server-side); interaksi data sangat cepat tanpa HTTP overhead.
- **Negatif:** Konsumsi memori server bertambah seiring bertambahnya koneksi sirkuit SignalR aktif; rentan terhadap jaringan yang sering putus total.  
  *Mitigasi:* Menerapkan alokasi RAM server yang memadai (~8-16 GB melayani 500+ sesi simultan), mengaktifkan reconnection retry policy, dan stateless navigation jika sirkuit terputus lama.

---

## ADR-004: Pemilihan MudBlazor sebagai Design System & UI Library

### Konteks Bisnis & Teknis
Sistem ERP membutuhkan puluhan komponen antarmuka standar korporasi: tabel data server-side dengan sorting/filtering/pagination kompleks, modal dialog konfirmasi, tree view untuk Chart of Accounts, date-picker, dan input currency yang presisi.

### Keputusan
Mengadopsi pustaka komponen **MudBlazor (Material Design for Blazor)** sebagai fondasi utama antarmuka pengguna.

### Konsekuensi & Mitigasi
- **Positif:** Desain visual konsisten, sangat modular, performa rendering tinggi, dokumentasi lengkap, dan dukungan kuat untuk server-side data loading.
- **Negatif:** Ketergantungan pada siklus rilis library eksternal.  
  *Mitigasi:* Seluruh komponen input kompleks dibungkus (*wrapper components*) di shared presentation layer untuk mengisolasi potensi breaking changes di masa depan.

---

## ADR-005: PostgreSQL Multi-Domain Schemas dalam Satu Database

### Konteks Bisnis & Teknis
ERP mengelola ratusan tabel bisnis. Jika semua tabel diletakkan di schema default `public`, basis data menjadi sulit dikelola, risiko penamaan bentrok tinggi, dan batas hak akses sulit ditegakkan. Memisahkan ke dalam multiple database independen juga memutus relasi foreign key dan menghancurkan integritas referensial.

### Keputusan
Menggunakan **satu database PostgreSQL terpadu** yang dipartisi secara logis menjadi 8 schema domain:
`identity`, `master`, `hr`, `inventory`, `procurement`, `sales`, `finance`, dan `audit`.

### Konsekuensi & Mitigasi
- **Positif:** Pengelompokan tabel sangat rapi; integritas referensial (Foreign Key) antar schema tetap bekerja sempurna; query join lintas domain dapat dieksekusi efisien oleh PostgreSQL query planner.
- **Negatif:** Konfigurasi migrasi EF Core memerlukan pendefinisian schema secara eksplisit di setiap entity.

---

## ADR-006: Entity Framework Core 10 & Npgsql sebagai ORM Utama

### Konteks Bisnis & Teknis
Dibutuhkan ORM yang mendukung pemetaan relasional secara typesafe, memiliki kemampuan LINQ query provider mutakhir, serta integrasi mendalam dengan kapabilitas native PostgreSQL (seperti tipe data JSONB, UUID, enum, dan concurrency token).

### Keputusan
Memilih **Entity Framework Core 10** dengan provider **Npgsql.EntityFrameworkCore.PostgreSQL 10.x**.

### Konsekuensi & Mitigasi
- **Positif:** Produktivitas tinggi, migrasi otomatis terkontrol, dukungan `HasQueryFilter` untuk multi-tenancy, interceptor transparan untuk audit logging.
- **Negatif:** Potensi query tidak efisien jika pengembang tidak memahami evaluasi query SQL EF Core.  
  *Mitigasi:* Melarang penggunaan eager loading tanpa batas (`Include` berantai), mewajibkan penggunaan `AsNoTracking()` untuk operasi Query read-only, dan menggunakan Dapper untuk query analitik berbobot tinggi jika diperlukan.

---

## ADR-007: ASP.NET Core Identity dengan Permission-Based Access Control

### Konteks Bisnis & Teknis
Pola otorisasi berbasis Role sederhana (*Role-Based Access Control / RBAC*) tidak cukup fleksibel untuk ERP korporat (misal: dua orang berstatus "Staf Sales", namun hanya staf senior yang berhak menyetujui diskon khusus).

### Keputusan
Menggunakan **ASP.NET Core Identity** sebagai fondasi autentikasi pengguna, dipadukan dengan **Permission-Based Access Control (PBAC)** granuler. Role hanya berfungsi sebagai pengelompok daftar izin (*container of permissions*).

### Konsekuensi & Mitigasi
- **Positif:** Administrator dapat mengatur hak akses per fungsi secara presisi tanpa perlu mengubah kode aplikasi.
- **Negatif:** Jumlah record permission dalam token atau sesi bertambah.  
  *Mitigasi:* Permission disimpan dalam memori sesi yang di-cache per user dan dievaluasi secara instan melalui custom authorization policy handler.

---

## ADR-008: Strategi Multi-Company & Multi-Branch berbasis Kolom Diskriminator

### Konteks Bisnis & Teknis
Terdapat 3 pendekatan umum multi-tenancy: (1) Database terpisah per tenant, (2) Schema terpisah per tenant, (3) Kolom diskriminator (`company_id` & `branch_id`) dalam tabel bersama. Pendekatan database/schema terpisah sangat menyulitkan konsolidasi laporan keuangan holding dan membutuhkan orkestrasi skrip migrasi berulang.

### Keputusan
Mengadopsi **Kolom Diskriminator Bersama (*Shared Table with Tenant Discriminator Column*)**. Setiap tabel transaksi bisnis wajib memiliki kolom `company_id` (NOT NULL) dan `branch_id` (NOT NULL).

### Konsekuensi & Mitigasi
- **Positif:** Laporan konsolidasi holding sangat mudah dibuat (cukup query dengan filter `company_id IN (...)`); pemeliharaan skema database dan migrasi hanya satu kali.
- **Negatif:** Risiko kebocoran data jika developer lupa menambahkan filter `WHERE company_id = ...`.  
  *Mitigasi:* Memasang **Global Query Filter EF Core** secara otomatis pada semua entitas bisnis saat pembentukan DbContext, sehingga filter cabang terinjeksi secara transparan di setiap query.

---

## ADR-009: Mekanisme Penomoran Dokumen Otomatis Terformat

### Konteks Bisnis & Teknis
Dokumen bisnis (PO, SO, Invoice, Jurnal) wajib memiliki nomor resmi yang berurutan, mudah dibaca, memuat identitas periode dan cabang, serta tidak boleh memiliki celah (*gapless sequence*) untuk keperluan audit perpajakan.

### Keputusan
Membangun tabel generator sekuensial khusus `master.document_sequences` dengan mekanisme *PostgreSQL Row-Level Locking* (`SELECT ... FOR UPDATE`) saat pencetakan nomor transaksi. Format nomor standar: `[PREFIX]-[YYYYMM]-[BRANCH_CODE]-[SEQUENCE_NUMBER]`.

### Konsekuensi & Mitigasi
- **Positif:** Penomoran dijamin urut, unik, dan tidak ada nomor ganda meskipun dua cabang membuat transaksi pada milidetik yang sama.
- **Negatif:** Potensi bottleneck singkat pada lock tabel sequence saat beban pembuatan transaksi sangat masif.  
  *Mitigasi:* Kunci baris (*row lock*) hanya dipegang dalam durasi transaksi mikro ($< 5\text{ ms}$) dan sequence dipartisi per tipe dokumen per cabang.

---

## ADR-010: Strategi Audit Trail Menggunakan EF Core SaveChanges Interceptor

### Konteks Bisnis & Teknis
ERP wajib mematuhi standar audit kepatuhan. Setiap mutasi data harus mencatat siapa yang mengubah, kapan, dari alamat IP mana, serta nilai sebelum dan sesudah perubahan.

### Keputusan
Mengimplementasikan **EF Core SaveChangesInterceptor (`AuditSaveChangesInterceptor`)**. Interceptor ini secara otomatis membedah *ChangeTracker* EF Core sebelum commit, mendeteksi entitas yang diubah, dan menyusun rekam jejak JSON ke dalam tabel `audit.audit_logs`.

### Konsekuensi & Mitigasi
- **Positif:** Tidak membutuhkan penulisan kode audit manual di setiap service/handler; 100% mutasi data terjamin tercatat otomatis.
- **Negatif:** Ukuran tabel audit membengkak seiring berjalannya waktu.  
  *Mitigasi:* Tabel audit dipartisi berdasarkan rentang tahun (*Range Partitioning*) dan data lama diarsipkan secara berkala melalui background job.

---

## ADR-011: Penegakan Keseimbangan Jurnal Akuntansi Double-Entry

### Konteks Bisnis & Teknis
Kesalahan paling fatal dalam sistem ERP adalah tersimpannya jurnal akuntansi yang tidak seimbang ($\sum Debit \ne \sum Credit$), yang berujung pada rusaknya laporan neraca.

### Keputusan
Menegakkan aturan keseimbangan pada 2 tingkat pertahanan:
1. **Domain Aggregate Invariant:** Method `JournalEntry.Post()` melempar `UnbalancedJournalDomainException` jika total debit tidak sama dengan total kredit.
2. **Database Check Constraint:** Memasang validasi transaksional bahwa jurnal berstatus 'Posted' tidak boleh memiliki selisih debit-kredit.

### Konsekuensi & Mitigasi
- **Positif:** Integritas finansial 100% terlindungi dari kegagalan logika software maupun modifikasi data liar.

---

## ADR-012: Pemrosesan Latar Belakang Menggunakan Hangfire

### Konteks Bisnis & Teknis
Operasi berat seperti perhitungan batch penggajian, pengiriman email massal, dan sinkronisasi penutupan buku tidak boleh dijalankan langsung di thread request Blazor Server karena akan membekukan UI pengguna.

### Keputusan
Mengadopsi **Hangfire** dengan driver persistensi PostgreSQL.

### Konsekuensi & Mitigasi
- **Positif:** Sangat stabil, mendukung penjadwalan berbasis Cron, memiliki dashboard pemantauan bawaan, dan mendukung retry otomatis jika terjadi kegagalan transient.
- **Negatif:** Membutuhkan skema tabel tambahan `hangfire.*` di database PostgreSQL.  
  *Mitigasi:* Skema Hangfire diisolasi tersendiri dan dipantau kinerjanya agar tidak membebani transaksi bisnis utama.

