# Cross-Module Transaction Scenarios & Data Integrity Traces

> **Document Version:** 1.0.0  
> **Purpose:** Validasi Integritas Transaksi Antarmodul & Konsistensi Jurnal Akuntansi  
> **Related Documents:** [../BUSINESS_WORKFLOWS.md](../BUSINESS_WORKFLOWS.md) | [../BUSINESS_RULES.md](../BUSINESS_RULES.md) | [../DATABASE_SCHEMA.md](../DATABASE_SCHEMA.md)

---

## Skenario 1: Penjualan Kredit Barang Dagang & Pelunasan Piutang (End-to-End O2C)

### 1. Profil Transaksi
- **Perusahaan:** PT Alpha Distribusi (Cabang Jakarta)
- **Pelanggan:** CV Maju Sentosa (Limit Kredit: Rp 100.000.000, Saldo Piutang Berjalan: Rp 20.000.000)
- **Produk:** Laptop Bisnis Pro (SKU: `LPT-001`, Saldo Fisik: 50 Unit, Rata-rata Biaya / HPP: Rp 8.000.000/unit)
- **Pesanan:** 10 Unit @ Rp 12.000.000 = Rp 120.000.000 + PPN 11% (Rp 13.200.000) = **Total Rp 133.200.000**.

---

### 2. Jejak Eksekusi & Mutasi Database Antarmodul

#### Langkah 1.1: Pembuatan Sales Order
- **Aksi:** Sales Executive menginput SO.
- **Pengecekan Bisnis:** Total piutang (Rp 20.000.000) + SO baru (Rp 133.200.000) = Rp 153.200.000 $>$ Limit Kredit (Rp 100.000.000).
- **Hasil:** Status SO menjadi `PendingApproval (Credit Limit Exceeded)`.
- **Approval:** Direktur Keuangan menyetujui dispensasi limit. Status SO menjadi `Confirmed`.
- **Dampak Database:**
  - `inventory.stock_balances` untuk `LPT-001` di Gudang Jakarta:  
    `physical_quantity` = 50, `reserved_quantity` bertambah dari 0 menjadi **10**. Stok Bebas = 40.

#### Langkah 1.2: Pengiriman Barang (Delivery Order / Surat Jalan)
- **Aksi:** Staf Gudang menerbitkan DO dan mengirim fisik 10 unit laptop.
- **Dampak Database:**
  - `sales.deliveries`: Record baru terbit (`DO-202611-JKT-0018`), status `Dispatched`.
  - `inventory.stock_balances`:  
    `physical_quantity` berkurang dari 50 menjadi **40**.  
    `reserved_quantity` berkurang dari 10 menjadi **0**.
  - `inventory.stock_movements`: Baris baru tercatat:  
    `movement_type = 'SalesDelivery'`, `direction = 'OUT'`, `qty = 10`, `unit_cost = 8.000.000`, `total_cost = 80.000.000`.
  - `finance.journal_entries`: Terbit Jurnal Pengakuan HPP otomatis:
    - **Debit:** Beban Pokok Penjualan (HPP): **Rp 80.000.000**
    - **Kredit:** Persediaan Barang Dagang: **Rp 80.000.000**
    - *Status Jurnal: Balanced & Posted ($\sum \text{Debit} = \sum \text{Kredit} = \text{Rp } 80.000.000$).*

#### Langkah 1.3: Penerbitan Faktur Penjualan (Sales Invoice)
- **Aksi:** Finance AR menerbitkan tagihan kepada CV Maju Sentosa.
- **Dampak Database:**
  - `sales.sales_invoices`: Record baru terbit (`INV-202611-JKT-0025`), total Rp 133.200.000, status `Posted`.
  - `finance.journal_entries`: Terbit Jurnal Komersial Penjualan otomatis:
    - **Debit:** Piutang Usaha (AR - CV Maju Sentosa): **Rp 133.200.000**
    - **Kredit:** Pendapatan Penjualan Barang: **Rp 120.000.000**
    - **Kredit:** Utang PPN Keluaran: **Rp 13.200.000**
    - *Status Jurnal: Balanced & Posted ($\sum \text{Debit} = \sum \text{Kredit} = \text{Rp } 133.200.000$).*

#### Langkah 1.4: Pembayaran Pelanggan (Payment Settlement)
- **Aksi:** CV Maju Sentosa mentransfer Rp 133.200.000 ke Rekening Bank BCA perusahaan.
- **Dampak Database:**
  - `finance.payments`: Record pembayaran masuk (`PAY-202611-JKT-0012`), status `Posted`.
  - `finance.payment_allocations`: Mengalokasikan Rp 133.200.000 ke `invoice_id = INV-202611-JKT-0025`.
  - `sales.sales_invoices`: Kolom `paid_amount` menjadi Rp 133.200.000, status berubah menjadi **`Paid`**.
  - `finance.journal_entries`: Terbit Jurnal Pelunasan Piutang:
    - **Debit:** Rekening Bank BCA Operasional: **Rp 133.200.000**
    - **Kredit:** Piutang Usaha (AR): **Rp 133.200.000**
    - *Status Jurnal: Balanced & Posted.*

---

## Skenario 2: Pengadaan Persediaan, Valuasi Moving Average, & 3-Way Match (P2P)

### 1. Profil Transaksi
- **Pemasok:** PT Tech Supplier Indonesia
- **Produk:** Mouse Optik Wireless (SKU: `MOU-002`)
- **Kondisi Awal Persediaan:**  
  - Stok Awal ($Q_{\text{awal}}$) = 100 Unit  
  - Biaya Rata-Rata Lama ($\bar{C}_{\text{lama}}$) = Rp 100.000 / unit  
  - Total Nilai Awal = Rp 10.000.000
- **Pembelian Baru:**  
  - Kuantitas Baru ($Q_{\text{masuk}}$) = 100 Unit  
  - Harga Beli Baru ($P_{\text{beli}}$) = Rp 120.000 / unit (Kenaikan harga)  
  - Subtotal = Rp 12.000.000 + PPN 11% (Rp 1.320.000) = **Total Rp 13.320.000**.

---

### 2. Jejak Mutasi & Rekalkulasi Biaya

#### Langkah 2.1: Penerimaan Barang Fisik (Goods Receipt Note)
- **Aksi:** Petugas gudang memverifikasi barang tiba dan menerbitkan GRN.
- **Kalkulasi Moving Average Cost Baru:**
  $$\bar{C}_{\text{baru}} = \frac{(100 \times 100.000) + (100 \times 120.000)}{100 + 100} = \frac{10.000.000 + 12.000.000}{200} = \mathbf{Rp\ 110.000\ /\ \text{unit}}$$
- **Dampak Database:**
  - `inventory.stock_balances`:  
    `physical_quantity` berubah dari 100 menjadi **200 Unit**.  
    `average_cost` diperbarui menjadi **Rp 110.000,00**.
  - `inventory.stock_movements`: Baris tercatat: `direction = 'IN'`, `qty = 100`, `unit_cost = 120.000`, `total_cost = 12.000.000`.
  - `finance.journal_entries`: Jurnal Penerimaan Barang (Akrual Liabilitas Sementara):
    - **Debit:** Persediaan Barang Dagang: **Rp 12.000.000**
    - **Kredit:** Persediaan Diterima Belum Ditagih (IRNI): **Rp 12.000.000**
    - *Status Jurnal: Balanced & Posted.*

#### Langkah 2.2: Verifikasi 3-Way Match & Faktur Pemasok (Vendor Bill)
- **Aksi:** Finance AP menerima faktur dari PT Tech Supplier Indonesia sebesar Rp 13.320.000.
- **Validasi 3-Way Matching:**
  - Item SKU: `MOU-002` cocok.
  - Qty: PO (100) = GRN (100) = Bill (100) $\rightarrow$ **MATCH**.
  - Harga: PO (Rp 120.000) = Bill (Rp 120.000) $\rightarrow$ **MATCH**.
- **Dampak Database:**
  - `procurement.vendor_bills`: Record dibuat (`BILL-202611-JKT-0009`), status `Posted`.
  - `finance.journal_entries`: Jurnal Pengakuan Utang Usaha Definitif:
    - **Debit:** Persediaan Diterima Belum Ditagih (IRNI): **Rp 12.000.000**
    - **Debit:** PPN Masukan: **Rp 1.320.000**
    - **Kredit:** Utang Usaha (Account Payable): **Rp 13.320.000**
    - *Akun IRNI lunas terserap ($12.000.000 - 12.000.000 = 0$). Jurnal Balanced.*

---

## Kesimpulan Pembuktian Integritas:
Kedua skenario di atas membuktikan bahwa seluruh transaksi operasional:
1. Menegakkan integritas referensial basis data PostgreSQL.
2. Tidak pernah menghasilkan stok fisik negatif.
3. Menghasilkan jurnal akuntansi yang selalu seimbang ($\sum \text{Debit} = \sum \text{Kredit}$).
4. Memperbarui kartu stok buku besar (*stock ledger*) secara konsisten dan transparan.

