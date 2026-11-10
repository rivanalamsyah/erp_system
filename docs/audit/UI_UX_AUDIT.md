# UI/UX & Design System Audit — Enterprise ERP System

> **Document ID:** AUD-UI-2026-001  
> **Component Library:** MudBlazor v9.0.0 (Material Design for Blazor)  
> **Related Documents:** [UI_UX_GUIDELINES.md](../UI_UX_GUIDELINES.md) | [CODING_STANDARDS.md](../CODING_STANDARDS.md)

---

## 1. Evaluasi Keselarasan Design System & Tema Visual

Sistem menggunakan tema kustom enterprise `ErpTheme.EnterpriseTheme` yang didefinisikan pada [`src/ERP.Web/Components/Themes/ErpTheme.cs`](file:///d:/erp_system/src/ERP.Web/Components/Themes/ErpTheme.cs):
- **Palet Warna Utama:** Deep Navy / Slate (`#0F172A`), Primary Blue (`#1E40AF`), Accent Cyan (`#0EA5E9`).
- **Surface & Background:** Bersih, terstruktur dengan abu-abu netral Slate (`#F8FAFC`).
- **Tipografi:** Menggunakan font sistem modern (*Inter / Roboto*) dengan hierarchy teks (`Typo.h5`, `Typo.subtitle2`, `Typo.caption`) yang konsisten.
- **MudBlazor v9 Compliance:** Kompatibilitas 100% tuntas; tidak ditemukan lagi penggunaan atribut usang `Dense` pada `MudNumericField`, `Title`/`AriaLabel` pada `MudIconButton`, atau `ColSpan` huruf kapital pada `MudTh`.

---

## 2. Temuan Audit UI/UX & Antarmuka Pengguna

### 2.1 `UI-003` [MEDIUM - P2]: Broken Navigation Link pada Menu Departemen
- **Komponen:** [`src/ERP.Web/Components/Layout/NavMenu.razor`](file:///d:/erp_system/src/ERP.Web/Components/Layout/NavMenu.razor#L29)
- **Tampilan:** Sidebar menampilkan menu *"Departemen"* di bawah kategori Master Data.
- **Masalah Aktual:**
  Menavigasi ke URL `/master/departments` memicu komponen `NotFound.razor` (Error 404).
- **Rekomendasi UI:**
  Buat file `src/ERP.Web/Components/Pages/Master/DepartmentsPage.razor` dengan layout standar:
  1. Header halaman dengan tombol *"Tambah Departemen"*.
  2. `MudTable` menampilkan Kode Departemen, Nama, Manajer, dan Status Aktif.
  3. `MudDialog` untuk form pembuatan dan pengeditan data departemen.

---

### 2.2 `UI-001` [MEDIUM - P2]: Indikator Cabang Bersifat Statis pada Top Bar
- **Komponen:** [`src/ERP.Web/Components/Layout/MainLayout.razor`](file:///d:/erp_system/src/ERP.Web/Components/Layout/MainLayout.razor#L26-L36)
- **Kondisi Aktual:**
  Top bar menampilkan teks statis:
  ```html
  <MudText Typo="Typo.body2" ...>
      HO-JKT &bull; Head Office Jakarta
  </MudText>
  ```
- **Kebutuhan Pengguna:**
  Pengguna dengan akses multi-cabang (misal Area Manager atau Direksi) membutuhkan kemampuan untuk melihat cabang aktif dan beralih cabang (*Branch Switcher*) secara dinamis dari dropdown top bar.
- **Rekomendasi Solusi:**
  Ubah komponen menjadi `MudMenu` interaktif:
  ```html
  <MudMenu AnchorOrigin="Origin.BottomCenter" TransformOrigin="Origin.TopCenter">
      <ActivatorContent>
          <div class="d-flex align-center cursor-pointer ...">
              <MudIcon Icon="@Icons.Material.Filled.LocationOn" Color="Color.Secondary" Class="mr-1" />
              <div>
                  <MudText Typo="Typo.caption">CABANG AKTIF</MudText>
                  <MudText Typo="Typo.body2">@_currentBranchName</MudText>
              </div>
          </div>
      </ActivatorContent>
      <ChildContent>
          @foreach (var branch in _userAllowedBranches)
          {
              <MudMenuItem OnClick="@(() => SwitchActiveBranch(branch.Id))">
                  @branch.Code - @branch.Name
              </MudMenuItem>
          }
      </ChildContent>
  </MudMenu>
  ```

---

### 2.3 `UI-002` [LOW - P3]: Badge Notifikasi Statis & Broken Profile Links
- **Komponen:** [`src/ERP.Web/Components/Layout/MainLayout.razor`](file:///d:/erp_system/src/ERP.Web/Components/Layout/MainLayout.razor#L39-L75)
- **Kondisi Aktual:**
  1. Badge notifikasi menampilkan angka hardcoded `"3"` tanpa flyout popover daftar pemberitahuan.
  2. Profil dropdown menampilkan teks statis `admin@enterprise.erp` dan menautkan ke `/security/profile` serta `/security/change-password` yang belum memiliki halaman implementasi (404).
- **Rekomendasi UI:**
  1. Hubungkan badge notifikasi ke data dinamis `NotificationService.GetUnreadCount()`.
  2. Tampilkan `CurrentUserService.UserName` atau `CurrentUserService.FullName`.
  3. Sediakan halaman `ProfilePage.razor` dan dialog `ChangePasswordDialog.razor`.

---

## 3. Evaluasi Kualitas Komponen Data & Form UX

| Aspek Komponen | Evaluasi Implementasi | Rekomendasi Peningkatan |
| :--- | :--- | :--- |
| **Data Tables (`MudTable`)** | Menggunakan `Hover="true"`, `Striped="true"`, `Dense="true"`. Terdapat `NoRecordsContent` untuk empty state. | Tambahkan sorting kolom interaktif pada kolom Tanggal dan Nilai Total. |
| **Input Dialogs (`MudDialog`)** | Modal form menggunakan `FullWidth="true"` dengan tombol aksi standar (Batal / Simpan). | Terapkan validasi inline (*real-time field validation*) sebelum submit form. |
| **Feedback Snackbars (`ISnackbar`)** | Notifikasi sukses (hijau) dan error (merah) konsisten di seluruh use case CRUD. | Pertahankan pola snackbar saat ini; pastikan durasi tampil 4-5 detik. |
| **Konfirmasi Aksi Destruktif** | Seluruh aksi pembatalan, penolakan, dan posting jurnal menggunakan dialog konfirmasi eksplisit. | Standarisasi tombol aksi: Gunakan warna `Color.Error` untuk aksi permanen. |
| **Responsivitas Mobile (`MudDrawer`)** | Sidebar menggunakan `Variant="DrawerVariant.Responsive"` yang menutup otomatis pada layar HP/tablet. | Pada tabel lebar (seperti Jurnal Lines atau SO Lines), sediakan scrollbar horizontal yang ramah sentuhan. |

