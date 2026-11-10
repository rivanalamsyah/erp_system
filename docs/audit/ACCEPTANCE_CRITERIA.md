# Measurable Acceptance Criteria — Remediation & Production Gates

> **Document ID:** AUD-ACC-2026-001  
> **Related Documents:** [TASK_BREAKDOWN.md](./TASK_BREAKDOWN.md) | [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)

---

## 1. Kriteria Keberterimaan Keamanan (Security Acceptance Criteria)

### AC-SEC-01: Validasi Token Antiforgery pada Login (`TSK-SEC-01`)
- **Skenario 1 (Gagal Tanpa Token CSRF):**
  - **Given:** Penyerang mengirimkan HTTP POST request ke `/api/auth/login` tanpa menyertakan request verification token antiforgery.
  - **When:** Request diterima oleh server.
  - **Then:** Server menolak request dengan status **HTTP 400 Bad Request** dan sesi autentikasi tidak terbentuk.
- **Skenario 2 (Sukses Dengan Token CSRF Sah):**
  - **Given:** Pengguna mengisi formulir login di browser dengan token antiforgery yang valid.
  - **When:** Formulir dikirimkan dengan kredensial yang benar.
  - **Then:** Server memvalidasi token, membuat cookie auth `SameSite=Strict; Secure; HttpOnly`, dan mengarahkan pengguna ke halaman Dashboard (`/`).

---

### AC-SEC-02: Rate Limiting Percobaan Login (`TSK-SEC-03`)
- **Skenario:**
  - **Given:** Alamat IP client mengirimkan permintaan login ke `/api/auth/login`.
  - **When:** Permintaan ke-6 dikirimkan dalam kurun waktu kurang dari 60 detik dari IP yang sama.
  - **Then:** Server segera memutus request dengan status **HTTP 429 Too Many Requests** beserta header `Retry-After`.

---

## 2. Kriteria Keberterimaan Basis Data & Kueri (Database Criteria)

### AC-DB-01: Performa Kueri Buku Besar Akun (`TSK-DB-01`)
- **Skenario:**
  - **Given:** Tabel `finance.journal_lines` berisi lebih dari 250.000 baris rekaman transaksi.
  - **When:** Kueri buku besar dijalankan untuk sebuah akun:
    ```sql
    EXPLAIN ANALYZE SELECT * FROM finance.journal_lines WHERE account_id = '...' ORDER BY created_at_utc DESC;
    ```
  - **Then:** Execution plan PostgreSQL menunjukkan **Index Scan** menggunakan `IX_journal_lines_account_id` dengan durasi eksekusi $< 15\text{ ms}$ (bukan Sequential Scan).

---

### AC-DB-02: Performa Kueri Kartu Stok Persediaan (`TSK-DB-02`)
- **Skenario:**
  - **Given:** Tabel `inventory.stock_movements` berisi lebih dari 500.000 riwayat mutasi stok.
  - **When:** Pengguna membuka rincian kartu stok produk tertentu di gudang tertentu.
  - **Then:** Data mutasi berhasil ditarik dan dirender di layar dalam waktu $< 250\text{ ms}$.

---

## 3. Kriteria Keberterimaan Fungsional & UI/UX (Functional & UI Criteria)

### AC-FUNC-01: Pengelolaan Data Departemen (`TSK-FUNC-01` & `TSK-UI-01`)
- **Skenario 1 (Navigasi Menu):**
  - **Given:** Pengguna terautentikasi berada di halaman mana saja.
  - **When:** Pengguna mengklik menu *"Departemen"* di sidebar Master Data.
  - **Then:** Browser diarahkan ke URL `/master/departments` dan menampilkan tabel data departemen yang aktif tanpa memicu error 404.
- **Skenario 2 (Pencatatan Departemen Baru):**
  - **Given:** Pengguna dengan izin `master.company.manage` membuka dialog *"Tambah Departemen"*.
  - **When:** Pengguna mengisi Kode Departemen unik (misal `ENG`), Nama (`Engineering & Maintenance`), dan memilih Cabang, lalu mengklik Simpan.
  - **Then:** Rekaman departemen tersimpan di database PostgreSQL, notifikasi sukses hijau muncul, dan baris departemen baru langsung tampil pada tabel.

---

### AC-UI-01: Peralihan Cabang Aktif Pengguna (`TSK-UI-02`)
- **Skenario:**
  - **Given:** Pengguna memiliki hak akses multi-cabang (Cabang Jakarta dan Cabang Surabaya).
  - **When:** Pengguna mengklik pemilih cabang di top bar dan memilih *"Cabang Surabaya"*.
  - **Then:** Teks cabang aktif di top bar berubah menjadi *"Cabang Surabaya"*, dan seluruh halaman data (misal persediaan gudang atau pesanan) memuat ulang data khusus Cabang Surabaya secara instan tanpa perlu logout.

---

## 4. Kriteria Keberterimaan Kesiapan Produksi (Go-Live Gate Criteria)

1. **Zero High/Critical Vulnerabilities:** Pemindaian keamanan otomatis menunjukkan nol kerentanan berisiko Tinggi atau Kritis.
2. **Kestabilan Blazor Circuit:** Uji konkurensi 100 circuit simultan selama 2 jam tanpa kebocoran memori (*memory leak*) atau crash process.
3. **Penyelesaian Seluruh Task P0 & P1:** Seluruh task bertanda prioritas P0 dan P1 pada `TASK_BREAKDOWN.md` berstatus **DONE**.
