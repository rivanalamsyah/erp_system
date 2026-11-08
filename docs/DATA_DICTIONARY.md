# Data Dictionary — Enterprise ERP System

> **Document Version:** 1.0.0  
> **Target Database Engine:** PostgreSQL 16+  
> **Related Documents:** [DATABASE_SCHEMA.md](./DATABASE_SCHEMA.md) | [DATABASE_DESIGN.md](./DATABASE_DESIGN.md) | [ENTITY_RELATIONSHIP_DIAGRAM.md](./ENTITY_RELATIONSHIP_DIAGRAM.md)

---

## 1. Schema: `identity` (Autentikasi & Hak Akses)

### 1.1 Tabel `identity.users`
Menyimpan identitas akun pengguna, kredensial password, dan status keamanan akun.

| Nama Kolom | Tipe Data | Null | Kunci / Referensi | Default | Keterangan & Aturan Bisnis |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `id` | `uuid` | NO | PK | `gen_random_uuid()` | Pengenal unik pengguna (UUIDv7). |
| `username` | `varchar(50)` | NO | UNIQUE | - | Nama pengguna unik untuk proses login. |
| `email` | `varchar(100)`| NO | UNIQUE | - | Alamat email terverifikasi untuk notifikasi dan reset password. |
| `password_hash` | `text` | NO | - | - | Hash password menggunakan PBKDF2 via ASP.NET Core Identity. |
| `security_stamp`| `text` | NO | - | - | Token acak untuk memvalidasi perubahan kredensial atau logout paksa. |
| `full_name` | `varchar(100)`| NO | - | - | Nama lengkap resmi pengguna. |
| `phone_number` | `varchar(20)` | YES | - | NULL | Nomor kontak telepon/WhatsApp pengguna. |
| `is_active` | `boolean` | NO | - | `TRUE` | Menandai apakah akun pengguna aktif atau dinonaktifkan. |
| `two_factor_enabled`| `boolean`| NO | - | `FALSE`| Status aktivasi 2FA berbasis aplikasi authenticator (TOTP). |
| `lockout_end_utc`| `timestamptz`| YES | - | NULL | Batas waktu akun terkunci akibat salah input password berulang. |
| `access_failed_count`|`integer`| NO | - | `0` | Jumlah akumulasi kegagalan percobaan login berturut-turut. |
| `created_at_utc`| `timestamptz`| NO | - | `CURRENT_TIMESTAMP`| Waktu akun didaftarkan (UTC). |
| `updated_at_utc`| `timestamptz`| YES | - | NULL | Waktu akun diperbarui terakhir (UTC). |
| `is_deleted` | `boolean` | NO | - | `FALSE` | Penanda soft-delete akun. |

---

### 1.2 Tabel `identity.roles`
Menyimpan daftar peran (*Roles*) yang mengelompokkan izin akses.

| Nama Kolom | Tipe Data | Null | Kunci / Referensi | Default | Keterangan & Aturan Bisnis |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `id` | `uuid` | NO | PK | `gen_random_uuid()` | Pengenal unik peran. |
| `name` | `varchar(50)` | NO | UNIQUE | - | Nama peran (misal: 'SuperAdmin', 'SalesManager', 'WarehouseStaff'). |
| `description` | `text` | YES | - | NULL | Penjelasan tanggung jawab dan cakupan tugas peran. |
| `is_system_role`| `boolean` | NO | - | `FALSE` | Peran bawaan sistem yang tidak boleh dihapus. |
| `created_at_utc`| `timestamptz`| NO | - | `CURRENT_TIMESTAMP`| Waktu peran dibuat (UTC). |

---

### 1.3 Tabel `identity.permissions`
Menyimpan daftar izin granuler (*fine-grained permissions*) untuk PBAC.

| Nama Kolom | Tipe Data | Null | Kunci / Referensi | Default | Keterangan & Aturan Bisnis |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `id` | `uuid` | NO | PK | `gen_random_uuid()` | Pengenal unik izin. |
| `code` | `varchar(100)`| NO | UNIQUE | - | Kode unik izin (misal: `sales.order.approve`, `finance.journal.post`). |
| `module` | `varchar(50)` | NO | - | - | Nama modul pemilik izin (misal: 'Sales', 'Finance'). |
| `description` | `text` | YES | - | NULL | Penjelasan aksi yang diotorisasi oleh izin ini. |
| `created_at_utc`| `timestamptz`| NO | - | `CURRENT_TIMESTAMP`| Waktu izin didaftarkan ke sistem (UTC). |

---

## 2. Schema: `master` (Data Induk & Struktur Organisasi)

### 2.1 Tabel `master.companies`
Menyimpan entitas legal perusahaan dalam struktur korporasi.

| Nama Kolom | Tipe Data | Null | Kunci / Referensi | Default | Keterangan & Aturan Bisnis |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `id` | `uuid` | NO | PK | `gen_random_uuid()` | Pengenal unik perusahaan legal. |
| `code` | `varchar(20)` | NO | UNIQUE | - | Kode singkat entitas (misal: 'CMP-ALPHA', 'PT-BETA'). |
| `name` | `varchar(150)`| NO | - | - | Nama resmi legal perusahaan sesuai akta pendirian. |
| `tax_identification_number`| `varchar(30)`| NO | - | - | NPWP 16 digit perusahaan. |
| `address` | `text` | NO | - | - | Alamat kantor pusat terdaftar. |
| `phone` | `varchar(20)` | YES | - | NULL | Nomor telepon kantor pusat. |
| `email` | `varchar(100)`| YES | - | NULL | Email resmi korespondensi korporat. |
| `currency_code` | `varchar(3)` | NO | - | `'IDR'` | Mata uang operasional dan pelaporan default. |
| `created_at_utc`| `timestamptz`| NO | - | `CURRENT_TIMESTAMP`| Waktu pembuatan data (UTC). |
| `created_by_user_id`| `uuid` | NO | FK $\rightarrow$ `identity.users(id)` | - | Pengguna yang membuat data. |
| `is_deleted` | `boolean` | NO | - | `FALSE` | Penanda soft-delete entitas perusahaan. |

---

### 2.2 Tabel `master.branches`
Menyimpan data kantor cabang operasional perusahaan.

| Nama Kolom | Tipe Data | Null | Kunci / Referensi | Default | Keterangan & Aturan Bisnis |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `id` | `uuid` | NO | PK | `gen_random_uuid()` | Pengenal unik kantor cabang. |
| `company_id` | `uuid` | NO | FK $\rightarrow$ `master.companies(id)` | - | Perusahaan induk tempat cabang ini bernaung. |
| `code` | `varchar(20)` | NO | UNIQUE (`company_id`, `code`) | - | Kode unik cabang dalam satu perusahaan (e.g., 'JKT-HO', 'SBY-01'). |
| `name` | `varchar(100)`| NO | - | - | Nama lengkap cabang. |
| `address` | `text` | YES | - | NULL | Lokasi alamat fisik cabang. |
| `phone` | `varchar(20)` | YES | - | NULL | Nomor kontak operasional cabang. |
| `is_head_office`| `boolean` | NO | - | `FALSE` | Menandai apakah cabang ini merupakan Kantor Pusat. |
| `is_deleted` | `boolean` | NO | - | `FALSE` | Penanda soft-delete cabang. |

---

### 2.3 Tabel `master.products`
Katalog barang dagangan, bahan baku, dan jasa.

| Nama Kolom | Tipe Data | Null | Kunci / Referensi | Default | Keterangan & Aturan Bisnis |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `id` | `uuid` | NO | PK | `gen_random_uuid()` | Pengenal unik produk. |
| `company_id` | `uuid` | NO | FK $\rightarrow$ `master.companies(id)` | - | Perusahaan pemilik produk. |
| `category_id` | `uuid` | NO | FK $\rightarrow$ `master.product_categories(id)`| - | Kategori klasifikasi produk. |
| `uom_id` | `uuid` | NO | FK $\rightarrow$ `master.units_of_measure(id)`| - | Satuan ukuran standar stok produk (e.g., PCS). |
| `sku` | `varchar(50)` | NO | UNIQUE (`company_id`, `sku`) | - | Stock Keeping Unit unik produk. |
| `barcode` | `varchar(50)` | YES | - | NULL | Kode barcode produk (EAN-13, UPC). |
| `name` | `varchar(200)`| NO | - | - | Nama lengkap deskriptif produk. |
| `product_type` | `varchar(20)` | NO | CHECK (`'Inventory'`, `'Service'`, `'RawMaterial'`) | - | Jenis penanganan perputaran produk. |
| `purchase_price`| `numeric(18,4)`| NO| CHECK ($\ge 0$) | `0` | Estimasi harga beli standar dari vendor. |
| `selling_price` | `numeric(18,4)`| NO| CHECK ($\ge 0$) | `0` | Harga jual standar kepada pelanggan. |
| `minimum_stock_level`|`numeric(14,4)`| NO| CHECK ($\ge 0$) | `0` | Batas stok minimum untuk pemicu reorder alert. |
| `is_active` | `boolean` | NO | - | `TRUE` | Menandai apakah produk masih dijual/digunakan. |
| `is_deleted` | `boolean` | NO | - | `FALSE` | Penanda soft-delete produk. |

---

## 3. Schema: `inventory` (Persediaan & Mutasi Stok)

### 3.1 Tabel `inventory.stock_balances`
Menyimpan saldo persediaan barang real-time per gudang.

| Nama Kolom | Tipe Data | Null | Kunci / Referensi | Default | Keterangan & Aturan Bisnis |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `id` | `uuid` | NO | PK | `gen_random_uuid()` | Pengenal unik saldo stok. |
| `company_id` | `uuid` | NO | FK $\rightarrow$ `master.companies(id)` | - | Perusahaan pemilik persediaan. |
| `warehouse_id` | `uuid` | NO | FK $\rightarrow$ `inventory.warehouses(id)` | - | Lokasi fisik gudang penyimpanan. |
| `product_id` | `uuid` | NO | FK $\rightarrow$ `master.products(id)` | - | Produk yang disimpan. |
| `physical_quantity`| `numeric(14,4)`| NO | - | `0` | Jumlah kuantitas aktual yang ada di dalam gudang. |
| `reserved_quantity`| `numeric(14,4)`| NO | CHECK ($\ge 0$) | `0` | Kuantitas yang terkunci untuk Sales Order yang dikonfirmasi. |
| `average_cost` | `numeric(18,4)`| NO | CHECK ($\ge 0$) | `0` | Biaya perolehan rata-rata tertimbang (*Moving Weighted Avg*). |
| `last_updated_at_utc`| `timestamptz`| NO | - | `CURRENT_TIMESTAMP` | Waktu mutasi terakhir pada saldo ini. |

---

### 3.2 Tabel `inventory.stock_movements`
Buku besar mutasi perpetual persediaan barang (**IMMUTABLE**).

| Nama Kolom | Tipe Data | Null | Kunci / Referensi | Default | Keterangan & Aturan Bisnis |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `id` | `uuid` | NO | PK | `gen_random_uuid()` | Pengenal unik baris mutasi. |
| `company_id` | `uuid` | NO | FK $\rightarrow$ `master.companies(id)` | - | Perusahaan pemilik transaksi. |
| `branch_id` | `uuid` | NO | FK $\rightarrow$ `master.branches(id)` | - | Cabang penyelenggara mutasi barang. |
| `warehouse_id` | `uuid` | NO | FK $\rightarrow$ `inventory.warehouses(id)` | - | Gudang tempat mutasi terjadi. |
| `product_id` | `uuid` | NO | FK $\rightarrow$ `master.products(id)` | - | Produk yang bermutasi. |
| `movement_type` | `varchar(30)` | NO | CHECK | - | Jenis mutasi (`PurchaseReceipt`, `SalesDelivery`, dll). |
| `quantity` | `numeric(14,4)`| NO | CHECK ($> 0$) | - | Kuantitas barang yang berpindah. |
| `direction` | `varchar(3)` | NO | CHECK (`'IN'`, `'OUT'`) | - | Arah perpindahan stok: Bertambah ('IN') / Berkurang ('OUT'). |
| `unit_cost` | `numeric(18,4)`| NO | CHECK ($\ge 0$) | - | Harga pokok per unit barang saat transaksi terjadi. |
| `total_cost` | `numeric(18,2)`| NO | CHECK ($\ge 0$) | - | Nilai moneter mutasi ($\text{qty} \times \text{unit cost}$). |
| `reference_number`| `varchar(50)` | NO | - | - | Nomor dokumen pemicu (Nomor GRN, Nomor Surat Jalan). |
| `movement_date_utc`| `timestamptz`| NO | - | `CURRENT_TIMESTAMP` | Waktu fisik mutasi dieksekusi di sistem. |

---

## 4. Schema: `finance` (Buku Besar & Pembukuan Berpasangan)

### 4.1 Tabel `finance.journal_entries`
Header jurnal transaksi akuntansi (**ENFORCE DOUBLE-ENTRY**).

| Nama Kolom | Tipe Data | Null | Kunci / Referensi | Default | Keterangan & Aturan Bisnis |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `id` | `uuid` | NO | PK | `gen_random_uuid()` | Pengenal unik transaksi jurnal. |
| `company_id` | `uuid` | NO | FK $\rightarrow$ `master.companies(id)` | - | Perusahaan pembukuan jurnal. |
| `branch_id` | `uuid` | NO | FK $\rightarrow$ `master.branches(id)` | - | Cabang asal transaksi. |
| `journal_number`| `varchar(50)` | NO | UNIQUE | - | Nomor resmi jurnal (e.g., `JV-202611-HO-0001`). |
| `journal_date` | `date` | NO | - | - | Tanggal pengakuan akuntansi transaksi. |
| `reference_type`| `varchar(30)` | NO | - | - | Tipe sumber transaksi (`SalesInvoice`, `GoodsReceipt`, dll). |
| `reference_number`| `varchar(50)`| NO | - | - | Nomor dokumen sumber terkait. |
| `description` | `text` | NO | - | - | Catatan narasi penjelasan ayat jurnal. |
| `status` | `varchar(20)` | NO | CHECK (`'Draft'`, `'Posted'`, `'Reversed'`) | - | Status siklus jurnal akuntansi. |
| `total_debit` | `numeric(18,2)`| NO | CHECK ($\ge 0$) | `0` | Total kumulatif debit jurnal. |
| `total_credit`| `numeric(18,2)`| NO | CHECK ($\ge 0$) | `0` | Total kumulatif kredit jurnal. |
| - | - | - | CHECK (`total_debit = total_credit`) | - | **Constraint:** Jurnal tidak boleh disimpan jika selisih. |

---

### 4.2 Tabel `finance.journal_lines`
Rincian ayat debit dan kredit per akun buku besar.

| Nama Kolom | Tipe Data | Null | Kunci / Referensi | Default | Keterangan & Aturan Bisnis |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `id` | `uuid` | NO | PK | `gen_random_uuid()` | Pengenal unik baris jurnal. |
| `journal_entry_id`| `uuid` | NO | FK $\rightarrow$ `finance.journal_entries(id)` | - | Header jurnal terkait. |
| `account_id` | `uuid` | NO | FK $\rightarrow$ `finance.chart_of_accounts(id)`| - | Akun buku besar yang didebit atau dikredit. |
| `debit` | `numeric(18,2)`| NO | CHECK ($\ge 0$) | `0` | Nominal debit baris (jika kredit > 0, debit wajib 0). |
| `credit` | `numeric(18,2)`| NO | CHECK ($\ge 0$) | `0` | Nominal kredit baris (jika debit > 0, credit wajib 0). |
| `description` | `text` | YES | - | NULL | Keterangan spesifik baris rekening jurnal. |

---

## 5. Schema: `audit` (Jejak Rekam Perubahan Data)

### 5.1 Tabel `audit.audit_logs`
Mencatat riwayat mutasi entitas data secara anti-manipulasi (*Partitioned*).

| Nama Kolom | Tipe Data | Null | Kunci / Referensi | Default | Keterangan & Aturan Bisnis |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `id` | `uuid` | NO | PK (Partisi) | `gen_random_uuid()` | Pengenal unik entri audit log. |
| `company_id` | `uuid` | NO | - | - | ID perusahaan tempat mutasi terjadi. |
| `entity_name` | `varchar(100)`| NO | - | - | Nama tabel/entitas yang diubah (e.g., 'sales_orders'). |
| `entity_id` | `uuid` | NO | - | - | ID baris rekaman entitas yang diubah. |
| `action` | `varchar(20)` | NO | CHECK (`'INSERT'`, `'UPDATE'`, `'DELETE'`, `'SOFT_DELETE'`) | - | Jenis operasi mutasi data. |
| `old_values` | `jsonb` | YES | - | NULL | Nilai seluruh kolom sebelum perubahan (format JSON). |
| `new_values` | `jsonb` | YES | - | NULL | Nilai seluruh kolom setelah perubahan (format JSON). |
| `user_id` | `uuid` | NO | - | - | ID pengguna yang mengeksekusi mutasi. |
| `ip_address` | `varchar(45)` | YES | - | NULL | Alamat IPv4 atau IPv6 client pengguna. |
| `created_at_utc`| `timestamptz`| NO | PK (Partisi) | `CURRENT_TIMESTAMP` | Waktu persis mutasi terjadi di server (UTC). |

