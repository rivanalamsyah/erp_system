# Functional Requirements Document (FRD) — Enterprise ERP System

> **Document Version:** 1.0.0  
> **Target Release:** v1.0.0-LTS  
> **Related Documents:** [PRODUCT_REQUIREMENTS.md](./PRODUCT_REQUIREMENTS.md) | [MODULE_SPECIFICATIONS.md](./MODULE_SPECIFICATIONS.md) | [BUSINESS_RULES.md](./BUSINESS_RULES.md) | [BUSINESS_WORKFLOWS.md](./BUSINESS_WORKFLOWS.md)

---

## 1. Pendahuluan & Taksonomi Kebutuhan Fungsional

Dokumen ini mendefinisikan secara menyeluruh seluruh kebutuhan fungsional (*Functional Requirements*) yang harus dipenuhi oleh Enterprise Resource Planning (ERP) System. Setiap kebutuhan diberi identifikasi unik dengan format:  
`FR-[DOMAIN]-[NOMOR]`

---

## 2. Katalog Kebutuhan Fungsional per Domain Modul

### 2.1 Domain 01: Core System, Multi-Tenancy & Governance (FR-SYS)

| ID Kebutuhan | Nama Kebutuhan | Aktor | Deskripsi Fungsional & Kriteria |
| :--- | :--- | :--- | :--- |
| **FR-SYS-001** | Multi-Company Management | Super Admin | Sistem wajib mendukung pembuatan dan pemeliharaan beberapa entitas legal perusahaan (*Company*) dalam satu instalasi sistem terpadu. |
| **FR-SYS-002** | Multi-Branch Management | Super Admin | Sistem wajib memungkinkan setiap perusahaan memiliki satu atau lebih unit cabang (*Branch*) dengan penomoran dokumen dan gudang tersendiri. |
| **FR-SYS-003** | Data Scoping & Context Switching | Seluruh Pengguna | Sistem wajib menyaring data transaksi sesuai hak akses cabang pengguna yang sedang aktif, dan mengizinkan perpindahan konteks cabang bagi yang berwenang. |
| **FR-SYS-004** | Document Numbering Engine | Sistem | Sistem wajib menyediakan generator nomor dokumen unik, sekuensial, anti-duplikasi, dan terformat (contoh: `[TYPE]-[YYYYMM]-[BRANCH]-[XXXX]`). |
| **FR-SYS-005** | Immutable Audit Trail Logging | Sistem | Sistem wajib mencatat setiap aktivitas INSERT, UPDATE, dan SOFT-DELETE secara otomatis ke dalam skema audit terisolasi (User, Waktu, IP, Data Lama vs Baru). |
| **FR-SYS-006** | Optimistic Concurrency Control | Sistem | Sistem wajib mendeteksi dan menolak perubahan data yang bertabrakan (*stale update*) menggunakan mekanisme token versi (*PostgreSQL xmin / row version*). |
| **FR-SYS-007** | Document Attachment Management | Operator Transaksi | Sistem wajib mendukung pengunggahan lampiran dokumen digital (PDF, JPG, PNG maks 5MB) pada transaksi pembelian, penjualan, dan klaim beban. |
| **FR-SYS-008** | In-App Real-Time Notification | Seluruh Pengguna | Sistem wajib menampilkan pemberitahuan instan via SignalR untuk dokumen yang memerlukan persetujuan (*approval*) atau saat status dokumen berubah. |

---

### 2.2 Domain 02: Master Data Management (FR-MDM)

| ID Kebutuhan | Nama Kebutuhan | Aktor | Deskripsi Fungsional & Kriteria |
| :--- | :--- | :--- | :--- |
| **FR-MDM-001** | Pengelolaan Unit of Measure (UoM) | Master Data Admin | Sistem dapat mengelola daftar satuan ukuran (PCS, BOX, KG, LTR, MTR) beserta rasio konversi antar satuan (contoh: 1 BOX = 24 PCS). |
| **FR-MDM-002** | Pengelolaan Kategori Produk | Master Data Admin | Sistem dapat mengelola struktur kategori dan sub-kategori produk secara hierarkis bertingkat. |
| **FR-MDM-003** | Pengelolaan Master Produk & Jasa | Master Data Admin | Sistem dapat mendaftarkan produk dengan atribut SKU unik, barcode, nama, tipe (Barang Jadi, Bahan Baku, Jasa), UoM standar, harga beli standar, dan harga jual standar. |
| **FR-MDM-004** | Pengelolaan Master Pelanggan | Sales Admin | Sistem dapat mendaftarkan pelanggan bisnis/individu dengan data NPWP, alamat penagihan, alamat pengiriman, batas kredit (*credit limit*), dan termin pembayaran (*term of payment*). |
| **FR-MDM-005** | Pengelolaan Master Pemasok (Supplier)| Proc Admin | Sistem dapat mengelola data vendor mencakup kontak, nomor rekening bank pembayaran, termin pembayaran standar, dan status aktif/non-aktif. |
| **FR-MDM-006** | Pengelolaan Master Gudang | Whs Admin | Sistem dapat mendaftarkan gudang fisik dan logis yang terikat ke cabang tertentu, lengkap dengan lokasi dan penanggung jawab gudang. |
| **FR-MDM-007** | Pengelolaan Departemen & Jabatan | HR Admin | Sistem dapat mengelola bagan organisasi mencakup departemen, divisi, dan level jabatan (*position levels*). |

---

### 2.3 Domain 03: Identity & Access Management (FR-IAM)

| ID Kebutuhan | Nama Kebutuhan | Aktor | Deskripsi Fungsional & Kriteria |
| :--- | :--- | :--- | :--- |
| **FR-IAM-001** | Autentikasi Pengguna & Password Policy | Pengguna | Autentikasi berbasis ASP.NET Core Identity dengan aturan kompleksitas password ketat (min 8 karakter, huruf besar, kecil, angka, simbol) dan hashing PBKDF2/Argon2. |
| **FR-IAM-002** | Proteksi Brute-Force & Account Lockout | Sistem | Akun pengguna wajib terkunci otomatis selama 15 menit apabila terjadi 5 kali kegagalan input password berturut-turut. |
| **FR-IAM-003** | Permission-Based Access Control (PBAC) | Security Admin | Sistem wajib mengevaluasi izin pengguna berdasarkan kombinasi Role dan izin spesifik (*fine-grained permissions*, e.g., `sales.order.approve`). |
| **FR-IAM-004** | Pembatasan Akses Cabang (Data Scoping) | Security Admin | Setiap akun pengguna dapat diasosiasikan dengan satu atau banyak cabang dan hanya dapat membaca transaksi pada cabang yang diizinkan. |
| **FR-IAM-005** | Manajemen Sesi & Auto-Logout | Sistem | Sesi Blazor Server yang tidak aktif selama 30 menit wajib secara otomatis diputus demi alasan keamanan (*session timeout*). |
| **FR-IAM-006** | Two-Factor Authentication (2FA/MFA) | Admin / Pengguna | Sistem mendukung pengaktifan 2FA berbasis TOTP (Google Authenticator / Microsoft Authenticator) khusus untuk akun level manajerial dan admin. |

---

### 2.4 Domain 04: Human Resource Management & Payroll (FR-HRM)

| ID Kebutuhan | Nama Kebutuhan | Aktor | Deskripsi Fungsional & Kriteria |
| :--- | :--- | :--- | :--- |
| **FR-HRM-001** | Pengelolaan Biodata Karyawan | HR Staff | Mencatat profil lengkap karyawan: NIK, nama, tanggal lahir, kontak, cabang, departemen, jabatan, tanggal mulai kerja, status kontrak/tetap, dan nomor rekening gaji. |
| **FR-HRM-002** | Pencatatan Kehadiran Harian | HR Staff / Sistem | Mencatat waktu check-in, check-out, keterlambatan, dan status kehadiran (Hadir, Izin, Sakit, Alpha). |
| **FR-HRM-003** | Manajemen Saldo & Pengajuan Cuti | Karyawan / Atasan | Karyawan dapat mengajukan permohonan cuti secara mandiri; atasan menerima notifikasi untuk menyetujui/menolak; sistem memotong kuota saldo cuti otomatis. |
| **FR-HRM-004** | Komponen Gaji Dinamis | HR Manager | Menetapkan komponen gaji: Gaji Pokok, Tunjangan Jabatan, Tunjangan Transport, Potongan BPJS Kesehatan/Ketenagakerjaan, dan Pajak PPh 21. |
| **FR-HRM-005** | Pemrosesan Batch Payroll Bulanan | HR Manager | Menjalankan kalkulasi gaji massal per periode bulan/tahun untuk seluruh karyawan cabang aktif dengan validasi potongan absensi. |
| **FR-HRM-006** | Integrasi Jurnal Beban Gaji ke Finance | Sistem | Saat batch payroll disetujui, sistem otomatis membentuk draf jurnal umum: Debit Beban Gaji & Tunjangan, Kredit Utang Gaji & Utang Pajak PPh 21. |
| **FR-HRM-007** | Penerbitan Slip Gaji Digital | Karyawan | Karyawan dapat mengunduh slip gaji pribadi dalam format PDF berproteksi password setelah periode payroll resmi diposting. |

---

### 2.5 Domain 05: Inventory & Warehouse Logistics (FR-INV)

| ID Kebutuhan | Nama Kebutuhan | Aktor | Deskripsi Fungsional & Kriteria |
| :--- | :--- | :--- | :--- |
| **FR-INV-001** | Kartu Stok Perpetual & Saldo Real-Time | Whs Staff / Mgr | Sistem wajib menyajikan buku besar mutasi persediaan barang (*Stock Ledger*) mencakup saldo awal, kuantitas masuk, kuantitas keluar, dan saldo akhir per gudang. |
| **FR-INV-002** | Penerimaan Barang dari PO | Whs Staff | Mencatat penerimaan fisik barang berdasarkan nomor Purchase Order dengan validasi kuantitas tidak boleh melebihi sisa outstanding PO. |
| **FR-INV-003** | Pengeluaran Barang untuk Surat Jalan | Whs Staff | Mencatat pengeluaran fisik barang berdasarkan nomor Sales Order yang disetujui, langsung memotong saldo stok gudang secara real-time. |
| **FR-INV-004** | Transfer Stok Antar Gudang | Whs Admin | Mendukung pengiriman stok antar gudang dengan alur 2 langkah: Pengiriman (*Stock Out Transit*) dan Konfirmasi Penerimaan (*Stock In Destination*). |
| **FR-INV-005** | Penyesuaian Stok (Stock Adjustment) | Whs Manager | Mencatat koreksi stok hasil stock opname fisik; selisih stok lebih atau kurang wajib memerlukan alasan penyesuaian dan approval manajer. |
| **FR-INV-006** | Valuasi Persediaan Otomatis | Sistem | Menghitung harga pokok perolehan persediaan menggunakan metode Rata-Rata Tertimbang (*Moving Weighted Average Cost*). |
| **FR-INV-007** | Ambang Batas Stok Minimum & Reorder Alert| Whs Supervisor | Menandai dan memberi peringatan otomatis di dashboard apabila saldo persediaan produk berada di bawah titik minimum pemesanan ulang (*Reorder Point*). |

---

### 2.6 Domain 06: Procurement & Supply Chain Management (FR-PRC)

| ID Kebutuhan | Nama Kebutuhan | Aktor | Deskripsi Fungsional & Kriteria |
| :--- | :--- | :--- | :--- |
| **FR-PRC-001** | Pembuatan Purchase Request (PR) | Staf Departemen | Mengajukan permohonan pembelian barang/jasa dengan rincian produk, perkiraan tanggal kebutuhan, jumlah yang diminta, dan justifikasi bisnis. |
| **FR-PRC-002** | Alur Persetujuan PR Berjenjang | Manajer Departemen | Manajer departemen dan manajer pengadaan meninjau, menyetujui, atau menolak dokumen PR sebelum dapat diproses menjadi Purchase Order. |
| **FR-PRC-003** | Penerbitan Purchase Order (PO) | Proc Officer | Mengonversi PR yang telah disetujui menjadi PO resmi yang ditujukan kepada vendor terpilih, mencakup kesepakatan harga beli, diskon, PPN, dan termin pembayaran. |
| **FR-PRC-004** | Validasi 3-Way Matching | Proc / Fin Staff | Sistem mencocokkan kuantitas dan harga antara: (1) Purchase Order, (2) Goods Receipt Note, dan (3) Supplier Invoice sebelum pembayaran diproses. |
| **FR-PRC-005** | Pengelolaan Faktur Pemasok (Vendor Bill)| Fin AP Staff | Mencatat tagihan utang usaha dari vendor berdasarkan bukti penerimaan barang yang sah dengan nomor seri faktur pajak vendor. |
| **FR-PRC-006** | Evaluasi Kinerja Pemasok | Proc Manager | Melacak ketepatan waktu pengiriman vendor (*On-Time Delivery Rate*) dan persentase barang cacat/retur. |

---

### 2.7 Domain 07: Sales & Commercial Operations (FR-SLS)

| ID Kebutuhan | Nama Kebutuhan | Aktor | Deskripsi Fungsional & Kriteria |
| :--- | :--- | :--- | :--- |
| **FR-SLS-001** | Pembuatan Penawaran Harga (Sales Quotation)| Sales Executive | Menerbitkan surat penawaran harga kepada calon pelanggan mencakup item produk, harga satuan, diskon, estimasi biaya kirim, dan masa berlaku penawaran. |
| **FR-SLS-002** | Konversi Quotation ke Sales Order | Sales Executive | Mengonversi penawaran yang disetujui menjadi Sales Order definitif; sistem langsung mengunci stok bebas (*reserve stock*) untuk pesanan tersebut. |
| **FR-SLS-003** | Validasi Limit Kredit Pelanggan | Sistem | Sistem secara otomatis menahan (*hold*) konfirmasi Sales Order jika total piutang berjalan ditambah nilai SO baru melampaui limit kredit pelanggan. |
| **FR-SLS-004** | Penerbitan Surat Jalan (Delivery Order) | Logistic Admin | Membuat dokumen pengiriman barang fisik berdasarkan nomor Sales Order; memvalidasi ketersediaan stok fisik di gudang pengirim. |
| **FR-SLS-005** | Penerbitan Faktur Penjualan (Sales Invoice)| Fin AR Staff | Menerbitkan faktur penagihan resmi kepada pelanggan berdasarkan barang yang telah terkirim (Delivery Order), lengkap dengan kalkulasi PPN 11%/12%. |
| **FR-SLS-006** | Pemantauan Status Pemenuhan Pesanan | Sales & Customer | Melacak status pesanan: *Draft*, *Confirmed*, *Partially Delivered*, *Fully Delivered*, *Invoiced*, *Closed*. |

---

### 2.8 Domain 08: Finance & Double-Entry Accounting (FR-FIN)

| ID Kebutuhan | Nama Kebutuhan | Aktor | Deskripsi Fungsional & Kriteria |
| :--- | :--- | :--- | :--- |
| **FR-FIN-001** | Bagan Akun Standar (Chart of Accounts) | Chief Accountant | Mengelola bagan akun hierarkis 5 level: Aset, Kewajiban, Ekuitas, Pendapatan, dan Beban dengan klasifikasi header dan posting account. |
| **FR-FIN-002** | Pengelolaan Periode Fiskal & Locking | Fin Manager | Menentukan tahun dan bulan fiskal buku besar; menutup (*lock*) periode lampau agar tidak ada transaksi susulan yang dapat mengubah angka laporan resmi. |
| **FR-FIN-003** | Jurnal Umum Manual & Validasi Seimbang | Fin Staff | Menginput ayat jurnal manual dengan validasi wajib: $\sum \text{Debit} = \sum \text{Kredit}$. Sistem memblokir penyimpanan jika ada selisih sekecil apapun. |
| **FR-FIN-004** | Otomasi Jurnal Transaksi Operasional | Sistem | Membentuk jurnal otomatis pada saat: (1) Penerimaan barang gudang, (2) Pengiriman barang penjualan, (3) Penerbitan faktur, (4) Pembayaran kas. |
| **FR-FIN-005** | Manajemen Kas & Rekening Bank | Fin Cashier | Mengelola mutasi akun kas kecil (*petty cash*) dan rekening giro bank, termasuk transfer internal antar-kas/bank. |
| **FR-FIN-006** | Penerimaan Pembayaran Pelanggan (AR Payment)| Fin Cashier | Mencatat penerimaan uang dari pelanggan dan mengalokasikannya ke satu atau lebih faktur penjualan terbuka (*open invoices*). |
| **FR-FIN-007** | Pembayaran Utang Pemasok (AP Payment) | Fin AP Staff | Mencatat pembayaran tagihan kepada vendor via transfer bank atau kas dan memperbarui saldo utang usaha yang bersangkutan. |
| **FR-FIN-008** | Laporan Keuangan Standar | Fin Director | Menghasilkan laporan keuangan real-time: Neraca Saldo (*Trial Balance*), Laba Rugi (*Income Statement*), dan Neraca (*Balance Sheet*). |

---

### 2.9 Domain 09: Executive Reporting & Business Intelligence (FR-REP)

| ID Kebutuhan | Nama Kebutuhan | Aktor | Deskripsi Fungsional & Kriteria |
| :--- | :--- | :--- | :--- |
| **FR-REP-001** | Executive Dashboard Utama | C-Level / Direktur | Visualisasi kartu KPI: Total Penjualan Bulan Ini, Margin Laba Kotor, Total Piutang Jatuh Tempo, Total Utang Jatuh Tempo, dan Posisi Saldo Kas/Bank. |
| **FR-REP-002** | Laporan Umur Piutang (AR Aging Report) | Fin Manager | Menampilkan klasifikasi piutang pelanggan berdasarkan durasi tunggakan: Lancar (0–30 hari), 31–60 hari, 61–90 hari, dan > 90 hari. |
| **FR-REP-003** | Laporan Umur Utang (AP Aging Report) | Fin Manager | Menampilkan jadwal jatuh tempo kewajiban pembayaran kepada vendor untuk perencanaan arus kas keluar. |
| **FR-REP-004** | Laporan Valuasi & Perputaran Persediaan | Whs / Fin Mgr | Menampilkan kuantitas stok per gudang dikalikan biaya rata-rata tertimbang, serta indikator perputaran stok (*Stock Turnover Ratio*). |
| **FR-REP-005** | Laporan Penjualan per Pelanggan & Produk | Sales Director | Analisis rincian volume penjualan, diskon yang diberikan, dan kontribusi profit per kategori produk dan segmen pelanggan. |
| **FR-REP-006** | Ekspor Laporan Standar Excel & PDF | Seluruh Pengguna | Menyediakan kemampuan pengunduhan seluruh tabel laporan ke format XLSX dan dokumen PDF siap cetak dengan kop surat resmi. |

