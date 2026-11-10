# Module Audit Matrix — Enterprise ERP System

> **Document ID:** AUD-MAT-2026-001  
> **Related Documents:** [SYSTEM_AUDIT_REPORT.md](./SYSTEM_AUDIT_REPORT.md) | [BUSINESS_RULES.md](../BUSINESS_RULES.md) | [MODULE_SPECIFICATIONS.md](../MODULE_SPECIFICATIONS.md)

---

## 1. Matriks Evaluasi Menyeluruh 9 Modul ERP

Evaluasi mencakup kelengkapan Domain Entity, Use Cases CQRS, Validasi FluentValidation, Penegakan Aturan Bisnis (BR), Halaman UI MudBlazor, Otorisasi PBAC, Jejak Audit, dan Automated Tests:

| No | Modul ERP | Sub-Modul / Fitur | Kelengkapan Fungsional | Aturan Bisnis (BR) | UI & UX | Audit & PBAC | Status Pengujian | Kesiapan UAT |
| :-: | :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **1** | **Master Data** | Perusahaan (Company) | 100% | Validasi NPWP & Mata Uang | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Cabang (Branch) | 100% | Isolasi Cabang | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Departemen (Department) | **30% (Gap)** | Hirarki Departemen | **404 (Missing UI)** | Partial | Domain Only | **PERLU PERBAIKAN** |
| | | Produk & Satuan (UOM) | 100% | SKU Unik & Tipe Stok | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Pelanggan (Customer) | 100% | Plafon Kredit & Termin | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Pemasok (Supplier) | 100% | Rekening Bank & Pajak | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| **2** | **Identity & PBAC** | Manajemen User & Role | 100% | ASP.NET Core Identity | Lengkap | SuperAdmin Guard | Unit & Integration | **SIAP** |
| | | Matriks Izin (Permissions)| 90% | Granular Per Module | Lengkap | PBAC Active | Architecture Test | **SIAP** |
| | | Profil & Ganti Password | **0% (Gap)** | Password Complexity | **Missing Route** | - | Not Tested | **PERLU PERBAIKAN** |
| | | Jejak Audit (Audit Logs) | 100% | Immutable Change Logs | Lengkap | Read-Only Guard | Integration Test | **SIAP** |
| **3** | **Persediaan (Inventory)**| Gudang (Warehouses) | 100% | Multi-Warehouse | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Saldo & Kartu Stok | 100% | `BR-INV-001` (Non-negatif) | Lengkap | Realtime Ledger | Unit & Integration | **SIAP** |
| | | Mutasi Stok (Movements) | 100% | Moving Average Cost | Lengkap | Append-Only | Unit & Integration | **SIAP** |
| | | Transfer Antar Gudang | 100% | In-Transit Stock | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Penyesuaian & Opname | 100% | Approval Persetujuan | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| **4** | **Pengadaan (Procurement)**| Permintaan Pembelian (PR)| 100% | Approval Flow | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Pesanan Pembelian (PO) | 100% | PPN 11% & Kalkulasi | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Penerimaan Barang (GRN) | 100% | Perpetual Stock Add | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Tagihan Vendor (Bills) | 100% | 3-Way Match & AP | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| **5** | **Penjualan (Sales)** | Penawaran Harga (SQ) | 100% | Masa Berlaku SQ | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Pesanan Penjualan (SO) | 100% | `BR-SALES-001` & `002` | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Surat Jalan (DO) | 100% | `BR-SALES-003` & Dispatch | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Faktur Penjualan (SI) | 100% | Piutang Usaha & Pajak | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| **6** | **Keuangan & Akuntansi** | Bagan Akun (COA) | 100% | 5 Kategori Akun | Lengkap | Normal Balance | Unit & Integration | **SIAP** |
| | | Jurnal Umum (JV) | 100% | `BR-ACC-001` (Keseimbangan)| Lengkap | Invariant Post | Unit & Integration | **SIAP** |
| | | Kas & Bank | 100% | Rekening Transaksi | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Pembayaran & Alokasi | 100% | `BR-ACC-003` (Alokasi Max)| Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Tutup Periode Fiskal | 100% | `BR-ACC-002` (Lock Period)| Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| **7** | **SDM & Payroll** | Data Karyawan | 100% | Status PKWT/Tetap | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Presensi & Absensi | 100% | Hitung Keterlambatan | Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Pengajuan Cuti | 100% | `BR-HR-001` (Kuota 12 hari)| Lengkap | PBAC Active | Unit & Integration | **SIAP** |
| | | Penggajian (Payroll) | 100% | `BR-HR-002` (THP Non-negatif)| Lengkap | BPJS & PPh 21 | Unit & Integration | **SIAP** |
| **8** | **Laporan & Dokumen** | Neraca Keuangan (BS) | 100% | Aset = Kewajiban + Ekuitas| Visual Chip | Read-Only | Unit Test | **SIAP** |
| | | Laba Rugi (P&L) | 100% | Omset - HPP - Beban | Lengkap | Read-Only | Unit Test | **SIAP** |
| | | Neraca Saldo | 100% | Debit = Kredit | Lengkap | Read-Only | Unit Test | **SIAP** |
| | | Analisis Penjualan | 100% | Top Customer & Produk | Lengkap | Read-Only | Unit Test | **SIAP** |
| | | Valuasi Stok Persediaan | 100% | Nilai Buku FIFO/Moving Avg| Lengkap | Read-Only | Unit Test | **SIAP** |
| **9** | **Dashboard & Settings** | Executive Dashboard | 100% | Multi-Module KPI MTD | Realtime | PBAC Active | Manual Verification | **SIAP** |
| | | Pengaturan Kebijakan | 100% | PPN, Termin, Sequences | Lengkap | SuperAdmin Only| Manual Verification | **SIAP** |

---

## 2. Analisis Fungsional Per Modul & Temuan Spesifik

### 2.1 Modul Master Data
- **Kekuatan:**
  - Isolasi data cabang dan perusahaan menggunakan EF Core Global Query Filters berjalan efektif pada seluruh entitas master.
  - Validasi ketat pada nomor NPWP/Tax Number dan format kode entitas unik.
- **Kelemahan & Temuan:**
  - `MOD-001`: Entitas `Department` memiliki tabel database dan entity di Domain, namun belum memiliki fitur CQRS (Command/Query) di Application layer dan halaman view di Web layer (`DepartmentsPage.razor`), sehingga link navigasi menghasilkan error 404.

### 2.2 Modul Persediaan (Inventory)
- **Kekuatan:**
  - Penegakan aturan `BR-INV-001` pada metode Domain [`StockBalance.DeductStock`](file:///d:/erp_system/src/ERP.Domain/Entities/Inventory/StockBalance.cs) secara deterministik melempar `InvalidOperationException` jika stok fisik tidak mencukupi, mencegah saldo negatif.
  - Setiap perubahan kuantitas menghasilkan record mutasi imutabel di `StockMovement` dengan kalkulasi biaya rata-rata tertimbang (*Moving Average Cost*).
- **Kelemahan & Temuan:**
  - `DB-002`: Belum tersedianya indeks komposit pada `stock_movements(warehouse_id, product_id, movement_date)` yang berpotensi memperlambat penarikan kartu stok saat data membesar.

### 2.3 Modul Pengadaan (Procurement)
- **Kekuatan:**
  - Alur lengkap PR $\to$ PO $\to$ GRN $\to$ Vendor Bill terhubung secara terstruktur.
  - 3-Way Matching tervalidasi pada penerimaan tagihan vendor terhadap nomor PO dan kuantitas fisik yang dicatat pada GRN.
- **Kelemahan & Temuan:**
  - `BIZ-002`: Otorisasi persetujuan PO (`PoApprove`) belum mendukung hierarki matriks limit nominal (misal: Supervisor < Rp 10 Juta, Manajer < Rp 100 Juta, Direktur > Rp 100 Juta).

### 2.4 Modul Penjualan (Sales & Distribution)
- **Kekuatan:**
  - Pengecekan limit kredit pelanggan (`BR-SALES-001`) dan reservasi persediaan (`BR-SALES-002`) berhasil mencegah pesanan melebihi pagu kredit maupun *overselling*.
  - Surat Jalan (Delivery Order) secara otomatis melepas kuantitas tereservasi dan memotong stok fisik gudang saat status berubah menjadi `Dispatched`.
- **Kelemahan & Temuan:**
  - `SEC-003`: Izin untuk melihat daftar SO (`GetSalesOrdersQuery`) saat ini membutuhkan klaim `sales.order.create` karena belum tersedianya klaim tersendiri `sales.order.view`.

### 2.5 Modul Keuangan & Akuntansi (Finance & Accounting)
- **Kekuatan:**
  - Keseimbangan mutlak jurnal akuntansi (`BR-ACC-001`) ditegakkan di level Domain (`JournalEntry.Post`). Upaya posting jurnal tidak seimbang diuji secara otomatis dan terbukti gagal sesuai aturan.
  - Proteksi periode fiskal tertutup (`BR-ACC-002`) mencegah manipulasi pembukuan periode lampau.
- **Kelemahan & Temuan:**
  - `DB-001`: Kurangnya indeks eksplisit pada kolom `finance.journal_lines.account_id` untuk mempercepat query buku besar akun.

### 2.6 Modul SDM & Penggajian (HR & Payroll)
- **Kekuatan:**
  - Perhitungan gaji bersih (`NetSalary`) secara ketat menolak nilai minus (`BR-HR-002`) dan menghitung komponen BPJS serta PPh 21 secara otomatis.
  - Pengajuan cuti membatasi pemakaian melebihi kuota 12 hari kalender (`BR-HR-001`).
- **Kelemahan & Temuan:**
  - Belum tersedia antarmuka integrasi mesin absensi biometrik otomatis (saat ini input manual atau via batch API).

### 2.7 Modul Laporan & Analitik (Reports)
- **Kekuatan:**
  - Laporan Neraca Keuangan secara dinamis membandingkan Total Aset dengan Total Kewajiban + Ekuitas, menyajikan indikator visual *Balanced / Imbalanced*.
  - Laporan Laba Rugi menghitung gross margin dan net margin secara akurat berdasarkan jurnal dan transaksi penjualan/pembelian.
- **Kelemahan & Temuan:**
  - `FUNC-003`: Tombol Export ke Excel/PDF belum terhubung ke engine generator dokumen biner (misal ClosedXML / QuestPDF).

---

## 3. Rekomendasi Prioritas Per Modul

1. **Master Data & Security (P0/P1):** Buat `DepartmentsPage.razor`, tambahkan rute `profile` dan `change-password`.
2. **Sales & Procurement (P1):** Perhalus granularitas permission (`view` vs `create`) dan tambahkan approval limit matrix.
3. **Finance & Inventory (P1):** Tambahkan indeks database pada tabel jurnal dan mutasi stok.
4. **Reports (P2):** Hubungkan library ekspor file Excel & PDF pada seluruh tab laporan.

