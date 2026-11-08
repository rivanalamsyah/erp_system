# Glossary of Terms — Enterprise ERP System

> **Document Version:** 1.0.0  
> **Classification:** Enterprise Reference Standard  
> **Related Documents:** [PROJECT_OVERVIEW.md](./PROJECT_OVERVIEW.md) | [BUSINESS_RULES.md](./BUSINESS_RULES.md) | [SOFTWARE_ARCHITECTURE.md](./SOFTWARE_ARCHITECTURE.md)

---

## 1. Istilah Rantai Pasok & Operasional (Supply Chain & Operations)

| Istilah | Singkatan | Definisi Konseptual & Penerapannya dalam ERP |
| :--- | :---: | :--- |
| **3-Way Matching** | - | Prosedur verifikasi silang tiga arah sebelum pembayaran disetujui: mencocokkan data pada *Purchase Order* (PO), *Goods Receipt Note* (GRN), dan *Vendor Bill*. |
| **Available-to-Promise** | ATP | Kuantitas barang di gudang yang belum dialokasikan untuk pesanan manapun dan siap dijanjikan kepada pelanggan baru ($\text{Stok Fisik} - \text{Stok Terkunci}$). |
| **Delivery Order** | DO | Surat Jalan pengiriman fisik barang dagang dari gudang perusahaan kepada pelanggan yang memotong saldo persediaan secara langsung. |
| **Goods Receipt Note** | GRN | Dokumen bukti penerimaan fisik barang dari vendor di gudang yang memicu penambahan saldo persediaan dan pencatatan utang sementara (IRNI). |
| **Moving Weighted Average** | MWA | Metode valuasi persediaan di mana harga pokok rata-rata per unit diperbarui setiap kali terjadi penerimaan barang baru dengan harga beli berbeda. |
| **Perpetual Inventory** | - | Sistem pencatatan persediaan yang memperbarui kuantitas dan nilai stok secara terus menerus setiap kali terjadi transaksi masuk atau keluar barang. |
| **Purchase Order** | PO | Dokumen kontrak pesanan resmi dari perusahaan yang dikirimkan kepada pemasok (*vendor*) untuk membeli barang atau jasa dengan klausul harga mengikat. |
| **Purchase Request** | PR | Dokumen permohonan internal dari suatu departemen kepada bagian pengadaan untuk meminta pembelian barang atau jasa. |
| **Reorder Point** | ROP | Ambang batas minimum saldo persediaan yang memicu sistem untuk memberikan peringatan perlunya dilakukan pemesanan barang kembali. |
| **Stock Adjustment** | - | Transaksi penyesuaian untuk menyelaraskan catatan saldo persediaan di sistem dengan hasil perhitungan fisik di gudang (*Stock Opname*). |
| **Stock Keeping Unit** | SKU | Kode alfanumerik unik pengenal produk yang membedakan jenis, ukuran, warna, dan spesifikasi barang. |
| **Stock Opname** | - | Kegiatan perhitungan fisik persediaan barang secara berkala di gudang untuk memverifikasi akurasi data sistem. |
| **Unit of Measure** | UoM | Satuan hitung kuantitas produk (contoh: PCS, BOX, KG, LTR, MTR) beserta faktor konversinya. |

---

## 2. Istilah Keuangan & Akuntansi (Finance & Accounting)

| Istilah | Singkatan | Definisi Konseptual & Penerapannya dalam ERP |
| :--- | :---: | :--- |
| **Account Payable** | AP | Utang usaha kepada pemasok atas pembelian barang atau jasa yang belum dibayar. |
| **Account Receivable** | AR | Piutang dagang kepada pelanggan atas penjualan barang atau jasa secara kredit. |
| **Aging Schedule** | - | Laporan pengelompokan piutang atau utang berdasarkan durasi keterlambatan jatuh tempo (0-30, 31-60, 61-90, dan >90 hari). |
| **Balance Sheet** | - | Neraca keuangan yang menyajikan posisi aset (*Assets*), kewajiban (*Liabilities*), dan ekuitas (*Equity*) perusahaan pada tanggal tertentu. |
| **Chart of Accounts** | COA | Daftar bagan akun terstruktur yang digunakan untuk mengklasifikasikan setiap pencatatan transaksi keuangan. |
| **Double-Entry Bookkeeping**| - | Prinsip pembukuan berpasangan di mana setiap transaksi wajib dicatat ke dalam minimal dua akun dengan total nilai Debit persis sama dengan Kredit. |
| **Fiscal Period** | - | Periode akuntansi bulanan atau tahunan tempat seluruh transaksi dibukukan; periode yang ditutup (*closed*) tidak dapat diubah lagi. |
| **General Ledger** | GL | Buku besar umum yang merangkum seluruh mutasi akun keuangan hasil posting jurnal. |
| **Income Statement (P&L)** | - | Laporan laba rugi yang menyajikan total pendapatan dikurangi beban pokok dan operasional dalam suatu rentang waktu. |
| **Inventory Received Not Invoiced** | IRNI | Akun liabilitas sementara yang menampung nilai barang yang telah tiba di gudang namun fakturnya belum diterima dari vendor. |
| **Journal Entry** | JV | Ayat pencatatan transaksi keuangan yang memuat akun debit, akun kredit, tanggal, dan dokumen referensi. |
| **Reversal Entry** | - | Jurnal pembalik yang diterbitkan untuk membatalkan atau mengoreksi dampak finansial dari transaksi masa lalu tanpa menghapus data aslinya. |
| **Trial Balance** | - | Neraca saldo yang memuat ringkasan saldo akhir seluruh akun untuk membuktikan keseimbangan debit dan kredit sebelum penyusunan laporan keuangan. |

---

## 3. Istilah Teknis & Arsitektur Perangkat Lunak (Technical & Architecture)

| Istilah | Definisi Teknis dalam Arsitektur Sistem |
| :--- | :--- |
| **Aggregate Root** | Pola Domain-Driven Design (DDD) di mana entitas utama mengontrol akses dan menegakkan invarian terhadap sekumpulan entitas anaknya (misal: `SalesOrder` mengontrol `SalesOrderItem`). |
| **Blazor Interactive Server** | Mode rendering Blazor di mana antarmuka pengguna dirender di sisi server dan interaksi ditransmisikan via koneksi WebSocket SignalR persisten. |
| **Circuit** | Sesi koneksi real-time dua arah yang dibuat oleh server Blazor untuk setiap tab browser pengguna aktif. |
| **Clean Architecture** | Pola arsitektur perangkat lunak yang memisahkan kode menjadi lapisan konsentris dengan aturan dependensi mengarah ke dalam (Domain adalah inti absolut). |
| **CQRS** | *Command Query Responsibility Segregation*: Pemisahan alur penulisan/modifikasi data (*Commands*) dari alur pembacaan data (*Queries*). |
| **Domain Event** | Pesan penanda peristiwa penting yang terjadi di lapisan Domain yang dapat dilanggani oleh modul lain untuk memicu efek samping in-process. |
| **Gapless Sequence** | Penomoran urut dokumen yang dijamin tidak memiliki angka yang terlewati/bolong demi memenuhi regulasi audit perpajakan. |
| **Invariant** | Aturan atau kondisi bisnis yang wajib bernilai benar (*true*) setiap saat sepanjang siklus hidup suatu entitas bisnis. |
| **Modular Monolith** | Arsitektur di mana aplikasi dikemas dalam satu unit deployment tunggal, namun secara internal dipartisi dengan batas modular yang tegas. |
| **Optimistic Concurrency** | Mekanisme kontrol konkurensi di mana sistem memeriksa apakah baris data telah diubah oleh pihak lain sebelum menyimpan modifikasi (`xmin`). |
| **PBAC** | *Permission-Based Access Control*: Model otorisasi yang mengevaluasi izin pengguna hingga level aksi spesifik, bukan sekadar nama peran. |
| **Result Pattern** | Pola penanganan hasil operasi yang mengembalikan objek penampung sukses/gagal secara eksplisit alih-alih melempar exception. |
| **Soft Delete** | Penghapusan secara logis dengan menandai `is_deleted = true` tanpa membuang baris data secara fisik dari basis data. |
| **UUIDv7** | Standar UUID berbasis waktu yang menyertakan timestamp milidetik pada 48-bit pertama sehingga tersusun sekuensial pada indeks basis data. |

