# Non-Functional Requirements Document (NFR) — Enterprise ERP System

> **Document Version:** 1.0.0  
> **Target Release:** v1.0.0-LTS  
> **Related Documents:** [PROJECT_OVERVIEW.md](./PROJECT_OVERVIEW.md) | [SYSTEM_DESIGN.md](./SYSTEM_DESIGN.md) | [SECURITY_DESIGN.md](./SECURITY_DESIGN.md) | [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)

---

## 1. Taksonomi & Metrik Kebutuhan Non-Fungsional

Dokumen ini menetapkan standar arsitektural dan operasional non-fungsional (*Non-Functional Requirements*) untuk memastikan sistem ERP beroperasi dengan kinerja tinggi, keandalan maksimal, keamanan enterprise, dan skalabilitas jangka panjang.

Setiap kebutuhan diberi kode unik: `NFR-[KATEGORI]-[NOMOR]`.

---

## 2. Kinerja & Waktu Respon (Performance & Responsiveness)

| ID Kebutuhan | Kategori | Parameter Kinerja | Target Kuantitatif |
| :--- | :--- | :--- | :--- |
| **NFR-PRF-001** | Latensi UI Server | Waktu bolak-balik SignalR (*Round-Trip Time / RTT*) untuk event interaktif biasa (klik tombol, input filter). | $\le 100\text{ ms}$ pada jaringan lokal; $\le 200\text{ ms}$ pada jaringan publik (RTT normal). |
| **NFR-PRF-002** | Waktu Muat Halaman | Rendering awal halaman data tabel (*First Contentful Paint* data list). | $\le 1.5\text{ detik}$ untuk 50 baris data pertama dengan pagination server-side. |
| **NFR-PRF-003** | Eksekusi Query Database | Waktu eksekusi query SQL transaksi operasional (*Read/Write operations*). | 95% query transaksi selesai dalam waktu $< 50\text{ ms}$; 99% query selesai $< 150\text{ ms}$. |
| **NFR-PRF-004** | Pemrosesan Batch Akhir Bulan | Waktu eksekusi perhitungan penggajian massal 500 karyawan atau kalkulasi penutupan periode. | $\le 30\text{ detik}$ dieksekusi secara asinkron di latar belakang (*background job*). |
| **NFR-PRF-005** | Ekspor Dokumen Laporan | Pembuatan file PDF atau Excel untuk laporan berukuran hingga 1.000 baris transaksi. | $\le 3\text{ detik}$ sampai proses download di browser dimulai. |

---

## 3. Skalabilitas & Konkurensi (Scalability & Concurrency)

| ID Kebutuhan | Aspek Skalabilitas | Spesifikasi Desain |
| :--- | :--- | :--- |
| **NFR-SCA-001** | Konkurensi Pengguna Aktif | Sistem mampu melayani sedikitnya **500 pengguna aktif konkuren** (*simultaneous active SignalR circuits*) tanpa degradasi performa pada spesifikasi server standar (8 vCPU, 16 GB RAM). |
| **NFR-SCA-002** | Throughput Transaksi Bisnis | Mampu memproses sedikitnya **100 transaksi bisnis per detik** (*Transactions Per Second / TPS*) pada beban puncak (*peak hours*). |
| **NFR-SCA-003** | Manajemen Koneksi Database | Menggunakan *Npgsql Connection Pooling* teroptimasi dengan batas default pool size 100 koneksi per instance aplikasi, dipadukan dengan PgBouncer untuk beban tinggi. |
| **NFR-SCA-004** | Skalabilitas Volume Data | Skema basis data dirancang untuk menangani pertumbuhan data hingga **10.000.000 baris transaksi per tahun** selama 5 tahun tanpa penurunan performa query berkat indexing B-Tree yang tepat dan strategi partisi tanggal pada skema audit dan jurnal. |

---

## 4. Keandalan & Ketersediaan Sistem (Reliability & Availability)

| ID Kebutuhan | Parameter | Standar Target |
| :--- | :--- | :--- |
| **NFR-REL-001** | Service Level Agreement (SLA) | Ketersediaan sistem operasional (*System Uptime*) minimal **99.9%** per tahun (toleransi downtime maksimal 8,76 jam per tahun di luar jadwal pemeliharaan terencana). |
| **NFR-REL-002** | Recovery Point Objective (RPO) | RPO $\le 15\text{ menit}$. Kehilangan data akibat insiden fatal sistem dibatasi maksimal data 15 menit terakhir melalui continuous PostgreSQL Write-Ahead Logging (WAL) archiving. |
| **NFR-REL-003** | Recovery Time Objective (RTO) | RTO $\le 2\text{ jam}$. Waktu pemulihan penuh sistem dari kondisi bencana (*Disaster Recovery*) hingga operasional kembali normal maksimal 120 menit. |
| **NFR-REL-004** | Graceful Disconnection Recovery | Sirkuit SignalR Blazor Server mendukung rekoneksi otomatis (*Automatic Circuit Reconnect*) hingga 60 detik saat terjadi gangguan jaringan mikro (*micro-disconnects*) pada client. |

---

## 5. Keamanan & Kepatuhan Regulasi (Security & Compliance)

| ID Kebutuhan | Aspek Keamanan | Implementasi & Standar Kepatuhan |
| :--- | :--- | :--- |
| **NFR-SEC-001** | Enkripsi Data Transit | Seluruh komunikasi HTTP dan WebSocket wajib menggunakan protokol **TLS 1.3** dengan sertifikat SSL/TLS valid. Akses HTTP otomatis dialihkan (*HTTP-to-HTTPS redirect*). |
| **NFR-SEC-002** | Enkripsi Data Istirahat | Basis data PostgreSQL di server produksi wajib dienkripsi pada level storage (*Encryption-at-Rest*) menggunakan LUKS (Linux Unified Key Setup) atau enkripsi disk AWS EBS / GCP Persistent Disk AES-256. |
| **NFR-SEC-003** | Pengelolaan Kredensial Pengguna | Kredensial password di-hash menggunakan algoritma PBKDF2 dengan salt unik per pengguna dan work factor yang direkomendasikan ASP.NET Core Identity. Plaintext password tidak pernah dicatat di log. |
| **NFR-SEC-004** | Otorisasi Berlapis (Defense-in-Depth) | Evaluasi permission dilakukan di lapisan Application Service (`AuthorizeAttribute` / manual PBAC check) dan filter query database otomatis (`HasQueryFilter` EF Core) untuk isolasi data cabang. |
| **NFR-SEC-005** | Mitigasi OWASP Top 10 | Bebas dari celah SQL Injection (menggunakan parameterized query via EF Core/Npgsql), Cross-Site Scripting (HTML auto-escaping Blazor), dan Cross-Site Request Forgery (Antiforgery token validasi server). |
| **NFR-SEC-006** | Kepatuhan Perlindungan Data (UU PDP) | Data pribadi sensitif (NIK karyawan, nomor rekening, nominal gaji) hanya dapat diakses oleh personil berwenang dan wajib disamarkan (*masked*) pada log audit umum. |

---

## 6. Auditabilitas & Ketertelusuran (Auditability & Traceability)

| ID Kebutuhan | Parameter | Ketentuan Teknis |
| :--- | :--- | :--- |
| **NFR-AUD-001** | Non-Repudiation Audit Logging | Setiap operasi perubahan data (*CUD: Create, Update, Delete*) wajib menyimpan audit trail mencakup: `id`, `table_name`, `entity_id`, `action`, `old_values` (JSONB), `new_values` (JSONB), `changed_by_user_id`, `client_ip`, dan `timestamp_utc`. |
| **NFR-AUD-002** | Imutabilitas Log Audit | Tabel `audit.audit_logs` diberi izin hanya INSERT bagi aplikasi; hak akses UPDATE dan DELETE dinonaktifkan secara ketat pada level privilege database PostgreSQL. |
| **NFR-AUD-003** | Ketertelusuran Siklus Dokumen | Setiap dokumen transaksi wajib menyimpan referensi balik (*traceability link*) ke dokumen sumbernya (contoh: Delivery Order menyimpan referensi `sales_order_id`, Sales Invoice menyimpan referensi `delivery_order_id`). |

---

## 7. Pemeliharaan & Kualitas Kode (Maintainability & Extensibility)

| ID Kebutuhan | Kriteria Desain | Ketentuan Arsitektural |
| :--- | :--- | :--- |
| **NFR-MNT-001** | Batasan Modularitas (Modular Boundaries) | Modul-modul bisnis tidak boleh memiliki dependensi siklis (*Zero Circular Dependencies*). Komunikasi antar modul lintas batas hanya melalui DTO atau Domain Events yang dikirim via mediator in-process. |
| **NFR-MNT-002** | Cakupan Pengujian Otomatis (Code Coverage)| Logika bisnis inti di lapisan `ERP.Application` dan `ERP.Domain` wajib memiliki cakupan unit test minimal **80%**. |
| **NFR-MNT-003** | Standar Dokumentasi API & Kode | Seluruh interface layanan publik wajib dilengkapi *XML documentation comments* standar C#. |
| **NFR-MNT-004** | Konfigurasi Terpusat Tanpa Hardcoding | Seluruh variabel konfigurasi lingkungan (koneksi database, batas timeout, email SMTP, secret key) wajib dibaca melalui `IConfiguration` dari Environment Variables atau Azure Key Vault / HashiCorp Vault. |

---

## 8. Observabilitas, Logging & Pemantauan (Observability & Monitoring)

| ID Kebutuhan | Aspek | Ketentuan |
| :--- | :--- | :--- |
| **NFR-OBS-001** | Structured Logging | Menggunakan **Serilog** dengan format output JSON terstruktur mencakup metadata kontekstual: `TraceId`, `SpanId`, `UserId`, `CompanyId`, dan `BranchId`. |
| **NFR-OBS-002** | Health Check Endpoint | Menyediakan endpoint bawaan ASP.NET Core `/health` (liveness probe) dan `/health/ready` (readiness probe yang memverifikasi konektivitas PostgreSQL). |
| **NFR-OBS-003** | Peringatan Anomali & Error | Log level `Error` dan `Fatal` secara otomatis memicu alert ke kanal operasional IT (email/webhook) untuk respon proaktif dalam waktu $< 5\text{ menit}$. |

