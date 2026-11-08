# Business Workflows Document — Enterprise ERP System

> **Document Version:** 1.0.0  
> **Related Documents:** [BUSINESS_RULES.md](./BUSINESS_RULES.md) | [FUNCTIONAL_REQUIREMENTS.md](./FUNCTIONAL_REQUIREMENTS.md) | [MODULE_SPECIFICATIONS.md](./MODULE_SPECIFICATIONS.md) | [DATABASE_SCHEMA.md](./DATABASE_SCHEMA.md)

---

## 1. Ikhtisar Alur Kerja Lintas Modul (End-to-End Enterprise Workflows)

Sistem ERP ini mengintegrasikan seluruh transaksi bisnis melalui 5 alur kerja operasional utama:
1. **Siklus Penjualan & Penerimaan Kas (Order-to-Cash / O2C)**
2. **Siklus Pengadaan & Pembayaran Utang (Procure-to-Pay / P2P)**
3. **Siklus Manajemen Persediaan & Transfer Logistik (Inventory & Logistics)**
4. **Siklus Kehadiran & Penggajian Karyawan (Hire-to-Payroll / H2P)**
5. **Siklus Penutupan Buku Akhir Periode (Record-to-Report / Month-End Close)**

---

## 2. Siklus Order-to-Cash (O2C) — Penjualan & Piutang

Alur penjualan mengatur proses dari permohonan harga pelanggan hingga penerimaan kas dan pembukuan jurnal akuntansi:

```mermaid
flowchart TD
    A([Mulai: Calon Pelanggan]) --> B[1. Buat Sales Quotation / SQ]
    B --> C{Pelanggan Setuju?}
    C -- Tidak / Revisi --> B
    C -- Ya --> D[2. Konfirmasi Menjadi Sales Order / SO]
    
    D --> E{Cek Limit Kredit Pelanggan}
    E -- Melebihi Limit --> F[Status: On-Hold / Butuh Approval Direktur]
    F --> G{Disetujui Direktur?}
    G -- Ditolak --> H([Pesanan Dibatalkan])
    G -- Disetujui --> I[3. Reservasi Stok di Gudang Terkait]
    E -- Masih Dalam Limit --> I
    
    I --> J[4. Terbitkan Delivery Order / Surat Jalan]
    J --> K[5. Fisik Barang Dikirim dari Gudang]
    K --> L[Otomasi: Potong Saldo Stok Fisik & Catat Stock Movement OUT]
    L --> M[Otomasi Jurnal: Dr. HPP / Cr. Persediaan Barang]
    
    K --> N[6. Terbitkan Faktur Penjualan / Sales Invoice]
    N --> O[Otomasi Jurnal: Dr. Piutang Usaha / Cr. Pendapatan Penjualan & Utang PPN]
    
    O --> P[7. Pelanggan Membayar Tagihan / Invoice]
    P --> Q[8. Catat Payment Receipt di Modul Finance]
    Q --> R[Otomasi Jurnal: Dr. Kas/Bank / Cr. Piutang Usaha]
    R --> S([Selesai: Transaksi Lunas & Ditutup])
```

### Rincian Tahapan & Efek Samping Database (O2C):
1. **Penerbitan Sales Order:** Status menjadi `Confirmed`. Kolom `reserved_quantity` pada tabel `inventory.stock_balances` bertambah sesuai kuantitas pesanan.
2. **Pengiriman Barang (Delivery Order Dispatched):**
   - Saldo fisik `physical_quantity` berkurang, `reserved_quantity` dilepaskan.
   - Tabel `inventory.stock_movements` mencatat baris baru berarah `OUT` dengan `movement_type = 'SalesDelivery'`.
   - Modul Finance membentuk ayat jurnal:
     - **Debit:** Beban Pokok Penjualan (HPP)
     - **Kredit:** Persediaan Barang Dagang
3. **Penerbitan Faktur Penjualan (Sales Invoice Posted):**
   - Menghasilkan piutang baru pada tabel `sales.sales_invoices`.
   - Modul Finance membentuk ayat jurnal komersial:
     - **Debit:** Piutang Usaha (AR)
     - **Kredit:** Pendapatan Penjualan
     - **Kredit:** Utang PPN Keluaran (11%/12%)
4. **Penerimaan Pembayaran (Customer Payment Receipt):**
   - Menghasilkan record di `finance.payments` dan `finance.payment_allocations`.
   - Mengurangi saldo `paid_amount` pada faktur.
   - Modul Finance membentuk ayat jurnal pelunasan:
     - **Debit:** Kas / Rekening Giro Bank
     - **Kredit:** Piutang Usaha

---

## 3. Siklus Procure-to-Pay (P2P) — Pengadaan & Utang

Alur pengadaan menjamin setiap pembelian barang memiliki otorisasi yang sah serta diverifikasi melalui pencocokan 3-arah (*3-Way Matching*):

```mermaid
flowchart TD
    A([Mulai: Kebutuhan Departemen]) --> B[1. Input Purchase Request / PR]
    B --> C{Approval Manajer Dept & Proc?}
    C -- Ditolak --> D([PR Ditolak])
    C -- Disetujui --> E[2. Konversi ke Purchase Order / PO]
    
    E --> F[3. Kirim PO Resmi ke Pemasok Terpilih]
    F --> G[4. Vendor Mengirim Barang ke Gudang]
    
    G --> H[5. Petugas Gudang Terima Barang: Terbitkan Goods Receipt Note / GRN]
    H --> I[Otomasi: Tambah Saldo Stok Fisik & Update Average Cost]
    I --> J[Otomasi Jurnal: Dr. Persediaan Barang / Cr. Hutang Belum Ditagih - IRNI]
    
    G --> K[6. Bagian Keuangan Terima Faktur Pemasok / Vendor Bill]
    K --> L{Validasi 3-Way Matching: PO vs GRN vs Bill}
    L -- Selisih Kuantitas / Harga --> M[Status: On-Hold / Investigasi Selisih]
    L -- Cocok Sempurna --> N[7. Setujui & Posting Faktur Vendor]
    N --> O[Otomasi Jurnal: Dr. Hutang Belum Ditagih - IRNI & Pajak Masukan / Cr. Utang Usaha - AP]
    
    N --> P[8. Bagian Keuangan Eksekusi Pembayaran Vendor / AP Payment]
    P --> Q[Otomasi Jurnal: Dr. Utang Usaha - AP / Cr. Kas/Bank]
    Q --> R([Selesai: PO Closed & Lunas])
```

### Jurnal Akuntansi Standar Pengadaan (P2P):
1. **Saat Penerimaan Fisik Barang di Gudang (Goods Receipt Note):**
   - **Debit:** Persediaan Barang Dagang
   - **Kredit:** Persediaan Diterima Belum Ditagih (*Inventory Received Not Invoiced / IRNI*)
2. **Saat Penerimaan Faktur Tagihan Vendor (Vendor Bill Posted):**
   - **Debit:** Persediaan Diterima Belum Ditagih (*IRNI*)
   - **Debit:** PPN Masukan
   - **Kredit:** Utang Usaha (Account Payable)
3. **Saat Pembayaran Tagihan Vendor (Vendor Bill Paid):**
   - **Debit:** Utang Usaha (Account Payable)
   - **Kredit:** Rekening Bank / Kas Operasional

---

## 4. Siklus Mutasi & Penyesuaian Persediaan (Inventory & Logistics)

```mermaid
flowchart TD
    subgraph TransferAntarGudang ["Alur 1: Transfer Antar Gudang"]
        T1[Gudang Asal: Buat Stock Transfer Request] --> T2[Gudang Asal: Dispatch Barang]
        T2 --> T3[Otomasi: Potong Stok Gudang Asal -> Status IN-TRANSIT]
        T3 --> T4[Gudang Tujuan: Terima & Inspeksi Fisik Barang]
        T4 --> T5[Otomasi: Tambah Stok Gudang Tujuan -> Status COMPLETED]
    end

    subgraph StockOpnameAdjustment ["Alur 2: Stock Opname & Penyesuaian"]
        O1[Jadwal Stock Opname Fisik] --> O2[Input Hasil Hitungan Fisik ke Sistem]
        O2 --> O3{Apakah Ada Selisih Fisik vs Sistem?}
        O3 -- Tidak Ada --> O4([Stok Akurat - Selesai])
        O3 -- Ada Selisih --> O5[Terbitkan Stock Adjustment Document]
        O5 --> O6{Approval Manajer Logistik & Finance}
        O6 -- Disetujui --> O7[Otomasi: Perbarui Saldo Stok Fisik Sistem]
        O7 --> O8[Otomasi Jurnal: Dr/Cr Beban Selisih Stok vs Cr/Dr Persediaan]
    end
```

---

## 5. Siklus Kehadiran & Penggajian Karyawan (Hire-to-Payroll)

```mermaid
flowchart TD
    A[Akumulasi Presensi Harian Mesin Biometrik/Aplikasi] --> B[Rekapitulasi Keterlambatan, Sakit, & Lembur]
    B --> C[Buka Periode Penggajian Bulanan / Payroll Period]
    C --> D[Eksekusi Batch Kalkulasi Gaji Otomatis]
    
    D --> E[Hitung: Gaji Pokok + Tunjangan Tetap + Lembur - Potongan Absensi - BPJS - PPh 21]
    E --> F[Review Rekap Payroll oleh HR Manager]
    F --> G{Approval Direktur HR & Keuangan?}
    G -- Ditolak --> D
    G -- Disetujui --> H[Posting Periode Payroll]
    
    H --> I[Otomasi Jurnal Finansial Beban Gaji]
    I --> J[Generate File Transfer Bank & Slip Gaji PDF Karyawan]
    J --> K[Eksekusi Transfer Penggajian via Bank Korporat]
    K --> L[Otomasi Jurnal: Dr. Utang Gaji / Cr. Rekening Bank]
    L --> M([Selesai: Gaji Terdistribusi])
```

### Jurnal Akuntansi Penggajian:
- **Debit:** Beban Gaji Karyawan
- **Debit:** Beban Tunjangan Karyawan
- **Debit:** Beban BPJS Ketenagakerjaan Porsi Perusahaan
- **Kredit:** Utang Gaji Karyawan (*Net Payroll Payable*)
- **Kredit:** Utang Pajak PPh 21 Karyawan
- **Kredit:** Utang Iuran BPJS Ketenagakerjaan/Kesehatan

---

## 6. Siklus Penutupan Buku Akhir Periode (Month-End Close)

```mermaid
flowchart TD
    A([Mulai Penutupan Buku Bulanan]) --> B[Verifikasi: Pastikan Tidak Ada Draf Dokumen Menggantung di Gudang/Sales/Proc]
    B --> C[Jalankan Valuasi Persediaan & HPP Akhir Bulan]
    C --> D[Input & Posting Jurnal Penyesuaian Penyusutan Aset & Akrual Beban]
    D --> E[Tarik Neraca Saldo / Trial Balance]
    E --> F{Total Debit = Total Kredit?}
    F -- Tidak Seimbang --> G[Investigasi Selisih Transaksi]
    G --> E
    F -- Seimbang Sempurna --> H[Approval CFO untuk Tutup Buku]
    H --> I[Kunci Periode Fiskal: finance.fiscal_periods is_closed = TRUE]
    I --> J[Sistem Blokir Seluruh Transaksi Bertanggal di Periode Terkunci]
    J --> K[Terbitkan Laporan Resmi: Neraca, Laba Rugi, & Arus Kas]
    K --> L([Selesai: Periode Ditutup Permanen])
```

