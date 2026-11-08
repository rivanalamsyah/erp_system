# Business Rules & Validation Specifications — Enterprise ERP System

> **Document Version:** 1.0.0  
> **Target Framework:** .NET 10 LTS | Clean Architecture (Domain & Application Rules)  
> **Related Documents:** [BUSINESS_WORKFLOWS.md](./BUSINESS_WORKFLOWS.md) | [FUNCTIONAL_REQUIREMENTS.md](./FUNCTIONAL_REQUIREMENTS.md) | [DATABASE_DESIGN.md](./DATABASE_DESIGN.md)

---

## 1. Taksonomi & Tingkat Penegakan Aturan Bisnis (Enforcement Tiers)

Seluruh aturan bisnis (*Business Rules*) dalam sistem ERP ditegakkan secara berlapis (*Multi-Tier Defense*):
1. **Tier 1 — UI Client Validation (MudBlazor Forms):** Memberikan umpan balik instan kepada pengguna saat mengisi form data (misal: kolom wajib tidak boleh kosong, kuantitas harus $> 0$).
2. **Tier 2 — Application Pipeline (FluentValidation):** Memvalidasi integritas DTO sebelum eksekusi Use Case dimulai.
3. **Tier 3 — Domain Entity Invariants (Core Engine):** Mengunci logika esensial dan konsistensi status transaksi di dalam method Domain Aggregate Root.
4. **Tier 4 — Database Constraints (PostgreSQL):** Lapisan pertahanan terakhir menggunakan Check Constraints, Foreign Keys, dan Unique Indexes.

Format identifikasi aturan: `BR-[DOMAIN]-[NOMOR]`.

---

## 2. Aturan Bisnis Umum Korporasi (BR-GEN)

### BR-GEN-001: Isolasi Data Cabang (Branch-Level Scoping)
- **Deskripsi:** Pengguna hanya diizinkan membaca, membuat, dan memodifikasi data transaksi bisnis pada cabang tempat pengguna tersebut terdaftar di `identity.user_branches`.
- **Pengecualian:** Pengguna yang memiliki atribut `is_head_office = true` atau memiliki hak akses konsolidasi (`holding.consolidation.view`) dapat membaca data seluruh cabang dalam mode *read-only*.
- **Penegakan:** Filter query global EF Core (`HasQueryFilter`) pada setiap query transaksi.

### BR-GEN-002: Penomoran Dokumen Sekuensial Tanpa Celah (Gapless Sequential Numbering)
- **Deskripsi:** Seluruh dokumen operasional dan akuntansi wajib memiliki format penomoran standar:  
  `[DOC_TYPE]-[YYYYMM]-[BRANCH_CODE]-[0001..9999]` (contoh: `SO-202611-JKT-0042`).
- **Aturan:** Penomoran tidak boleh memiliki celah (*no missing sequence numbers*). Jika pembuatan dokumen dibatalkan sebelum tersimpan ke database, nomor urut tidak boleh terbuang.
- **Penegakan:** Baris sekuensial dikunci menggunakan `SELECT ... FOR UPDATE` pada tabel `master.document_sequences` saat transaksi database aktif.

### BR-GEN-003: Imutabilitas Transaksi Finansial (Financial Immutability)
- **Deskripsi:** Dokumen yang telah berstatus `Posted` pada modul Keuangan, Pengadaan (Vendor Bill), Penjualan (Sales Invoice), dan Pergudangan (Stock Movement) **DILARANG DIHAPUS MAUPUN DIUBAH**.
- **Koreksi:** Setiap pembetulan transaksi harus dilakukan dengan menerbitkan dokumen penyesuaian (*Credit Note*, *Debit Note*, atau Jurnal Pembalik).

---

## 3. Aturan Bisnis Penjualan & Pelanggan (BR-SALES)

### BR-SALES-001: Pencegahan Penjualan Melebihi Batas Kredit (Credit Limit Enforcement)
- **Deskripsi:** Sistem wajib memvalidasi batas kredit pelanggan sebelum sebuah Sales Order dikonfirmasi.
- **Formula Validasi:**
  $$\text{Total Tagihan Terbuka (Unpaid Invoices)} + \text{Nilai SO yang Baru} \le \text{Credit Limit Pelanggan}$$
- **Tindakan:** Jika melebihi batas kredit, status Sales Order otomatis menjadi `On-Hold (Credit Limit Exceeded)`. Dokumen tidak dapat diproses ke tahap pengiriman barang sebelum mendapat persetujuan eksplisit dari Direktur Keuangan.

### BR-SALES-002: Pencegahan Penjualan Barang Tanpa Stok Bebas (No Overselling)
- **Deskripsi:** Sales Order hanya dapat dikonfirmasi jika kuantitas barang yang dipesan $\le$ kuantitas stok bebas (*Available Stock*) di gudang cabang pemesan:
  $$\text{Stok Bebas} = \text{Stok Fisik (physical\_quantity)} - \text{Stok Terkunci (reserved\_quantity)}$$
- **Tindakan:** Jika stok bebas kurang dari pesanan, sistem menolak konfirmasi order dan menyarankan penerbitan Purchase Request atau transfer stok dari gudang lain.

### BR-SALES-003: Validasi Penerbitan Surat Jalan (Delivery Order Consistency)
- **Deskripsi:** Total kuantitas barang yang dikirim dalam seluruh Surat Jalan untuk satu baris Sales Order dilarang keras melebihi kuantitas yang dipesan pada Sales Order tersebut:
  $$\sum \text{quantity\_shipped} \le \text{quantity\_ordered}$$

---

## 4. Aturan Bisnis Pengadaan & Pemasok (BR-PROC)

### BR-PROC-001: Matriks Otorisasi Persetujuan Pembelian (PR/PO Approval Matrix)
- **Deskripsi:** Persetujuan dokumen Purchase Request (PR) dan Purchase Order (PO) mengikuti batas nominal berikut:

| Rentang Nilai Dokumen (IDR) | Tingkat Persetujuan yang Diwajibkan |
| :--- | :--- |
| $\le \text{Rp } 10.000.000$ | Manajer Departemen Terkait |
| $\text{Rp } 10.000.001 - \text{Rp } 50.000.000$ | Manajer Departemen + Procurement Manager |
| $\text{Rp } 50.000.001 - \text{Rp } 250.000.000$ | Procurement Manager + Direktur Operasional (COO) |
| $> \text{Rp } 250.000.000$ | Direktur Keuangan (CFO) + Direktur Utama (CEO) |

### BR-PROC-002: Batas Toleransi Pencocokan 3-Arah (3-Way Matching Tolerances)
- **Deskripsi:** Saat memproses Faktur Tagihan Vendor (*Vendor Bill*), sistem memeriksa kesesuaian antara PO, Bukti Penerimaan Barang (GRN), dan Faktur Vendor:
  1. **Kuantitas:** Kuantitas ditagih wajib $\le$ kuantitas barang yang telah diterima di GRN.
  2. **Harga Satuan:** Toleransi perbedaan harga beli maksimal adalah **$0.0\%$** (sama persis dengan harga kontrak PO) kecuali disetujui secara manual oleh Purchasing Manager.

---

## 5. Aturan Bisnis Persediaan & Gudang (BR-INV)

### BR-INV-001: Larangan Saldo Stok Negatif (Zero Negative Stock Invariant)
- **Deskripsi:** Saldo fisik persediaan (`physical_quantity`) pada tabel `inventory.stock_balances` **DILARANG MENJADI KURANG DARI NOL ($< 0$)**.
- **Penegakan:** Database CHECK constraint dan domain validation sebelum baris mutasi keluar dieksekusi. Jika transaksi mutasi menyebabkan saldo $< 0$, seluruh transaksi dibatalkan (*Rollback*).

### BR-INV-002: Kalkulasi Valuasi Rata-Rata Tertimbang (Weighted Average Costing)
- **Deskripsi:** Biaya rata-rata persediaan diperbarui setiap kali terjadi penerimaan barang baru (*Goods Receipt*):
  $$\bar{C}_{\text{baru}} = \frac{(Q_{\text{awal}} \times \bar{C}_{\text{lama}}) + (Q_{\text{masuk}} \times P_{\text{beli}})}{Q_{\text{awal}} + Q_{\text{masuk}}}$$
- **Aturan Kasus Khusus:** Jika $Q_{\text{awal}} = 0$, maka $\bar{C}_{\text{baru}} = P_{\text{beli}}$. Transaksi mutasi barang keluar (*Sales Delivery*) tidak mengubah nilai $\bar{C}$, melainkan menggunakan nilai $\bar{C}$ berjalan sebagai dasar pengakuan Beban Pokok Penjualan (HPP).

---

## 6. Aturan Bisnis Keuangan & Buku Besar (BR-ACC)

### BR-ACC-001: Keseimbangan Mutlak Jurnal Akuntansi (Zero Imbalance Invariant)
- **Deskripsi:** Setiap ayat jurnal akuntansi wajib memiliki total nilai debit yang persis sama dengan total nilai kredit:
  $$\sum_{i=1}^{n} \text{Debit}_i = \sum_{j=1}^{m} \text{Credit}_j$$
- **Penegakan:**
  - Validasi di method domain `JournalEntry.Post()`.
  - Database constraint: `CONSTRAINT chk_journal_balance CHECK (total_debit = total_credit)`.
  - Minimal memiliki 2 baris jurnal (1 sisi debit dan 1 sisi kredit).

### BR-ACC-002: Larangan Transaksi pada Periode Fiskal yang Terkunci
- **Deskripsi:** Sistem menolak seluruh pembuatan, pengubahan, atau pemostingan transaksi yang memiliki tanggal transaksi pada periode fiskal yang statusnya telah ditutup (`is_closed = true`).
- **Penegakan:** Validator memeriksa tabel `finance.fiscal_periods` berdasarkan tahun dan bulan transaksi.

### BR-ACC-003: Batas Alokasi Pembayaran (Payment Allocation Limit)
- **Deskripsi:** Total pembayaran yang dialokasikan ke suatu faktur dilarang melebihi sisa tagihan terbuka faktur tersebut:
  $$\text{allocated\_amount} \le (\text{total\_amount} - \text{paid\_amount})$$

---

## 7. Aturan Bisnis Kepegawaian & Penggajian (BR-HR)

### BR-HR-001: Batas Kuota Cuti Tahunan
- **Deskripsi:** Karyawan tidak dapat mengajukan cuti tahunan jika jumlah hari yang diminta melebihi sisa saldo hak cuti tahunan berjalan yang dimiliki, kecuali permohonan diajukan sebagai Cuti Khusus Tanpa Gaji (*Unpaid Leave*).

### BR-HR-002: Gaji Bersih Tidak Boleh Negatif (Non-Negative Net Salary)
- **Deskripsi:** Total akumulasi potongan (potongan absensi, keterlambatan, pinjaman karyawan, BPJS, PPh 21) dilarang melebihi total penghasilan bruto (gaji pokok + tunjangan).
- **Aturan:**
  $$\text{Net Salary} = \text{Penghasilan Bruto} - \text{Total Potongan} \ge 0$$

