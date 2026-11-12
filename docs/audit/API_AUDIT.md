# API & CQRS PIPELINE AUDIT — Evaluasi Endpoint HTTP, Kontrak MediatR, Validasi & Error Handling

> **Tanggal Audit:** 12 November 2026  
> **Cakupan:** Minimal API Endpoints (`Program.cs`) & 67 MediatR CQRS Requests (`Commands` & `Queries`) di `src/ERP.Application`

---

## 1. Arsitektur Komunikasi & API Surface

Karena **Enterprise ERP System** dibangun sebagai **Blazor Web App (Interactive Server)** di atas satu proses Modular Monolith:
1. **HTTP Minimal API Endpoints (`src/ERP.Web/Program.cs:88-118`):** Digunakan khusus untuk siklus autentikasi cookie HTTP (`POST /api/auth/login` dan `POST /api/auth/logout`) karena cookie autentikasi (`HttpContext.Response.Cookies`) hanya dapat ditulis pada HTTP Request-Response biasa, bukan melalui koneksi WebSocket SignalR Blazor.
2. **In-Process CQRS Application API (`MediatR ISender`):** Seluruh komunikasi data dari komponen UI ke layer bisnis dilakukan melalui **67 kontrak `IRequest<TResponse>`** (42 Commands dan 25 Queries) yang melewati `ValidationPipelineBehavior` dan `AuthorizationPipelineBehavior`.

---

## 2. Audit Endpoint HTTP Minimal API (`Program.cs`)

| Endpoint | Method | Lokasi | Rate Limiting | Antiforgery / CSRF | Temuan & Catatan Audit |
| :--- | :---: | :--- | :---: | :---: | :--- |
| `/api/auth/login` | `POST` (`[FromForm]`) | `Program.cs:88-112` | `"LoginPolicy"` (5 req/menit) | `.DisableAntiforgery()` | 1. Rate limiter tidak dipartisi per IP (`SEC-HIGH-02`).<br/>2. Parameter `returnUrl` tidak divalidasi dengan `Uri.IsWellFormedUriString` / `LocalRedirect`, namun aman karena saat ini di-hardcode `Results.Redirect("/")`.<br/>3. Menggunakan `lockoutOnFailure: true` (baik). |
| `/api/auth/logout` | `POST` | `Program.cs:114-118` | Tidak ada | Tidak divalidasi (`SEC-MED-01`) | Tidak memeriksa apakah pemanggil terautentikasi dan tidak memvalidasi token Anti-CSRF dari `MainLayout.razor:115`. |

---

## 3. Matriks Audit Kontrak CQRS MediatR (67 Requests)

### 3.1 Ketidakkonsistenan Kontrak Return Type (`API-HIGH-01` / `ARCH-MED-01`)
Dari 67 request MediatR yang terdaftar di `src/ERP.Application/Features/`:
- **65 Request** mengembalikan `Result` atau `Result<T>`.
- **2 Request** mengembalikan `PagedResult<T>` secara langsung tanpa pembungkus `Result<T>`:
  1. `GetGoodsReceiptsQuery` (`src/ERP.Application/Features/Procurement/GoodsReceipts/GetGoodsReceiptsQuery.cs:15`) -> `IRequest<PagedResult<GoodsReceiptDto>>`
  2. `GetVendorBillsQuery` (`src/ERP.Application/Features/Procurement/VendorBills/GetVendorBillsQuery.cs:15`) -> `IRequest<PagedResult<VendorBillDto>>`

**Dampak Fatal pada Pipeline:**
Di dalam `ValidationPipelineBehavior.cs:61` dan `AuthorizationPipelineBehavior.cs:67`, metode `CreateFailureResult<TResponse>` memeriksa:
```csharp
if (typeof(TResponse) == typeof(Result)) return (TResponse)(object)Result.Failure(error);
if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>)) { ... }
throw new UnauthorizedAccessException(error.Description); // atau ValidationException
```
Karena `PagedResult<GoodsReceiptDto>` bukan `Result<>`, kegagalan izin pada kedua Query tersebut langsung melempar *unhandled exception* yang memutus koneksi Blazor Server!

---

### 3.2 Cakupan Validator Input (`FluentValidation`) (`API-MED-01`)
Hasil pemeriksaan seluruh kelas `AbstractValidator<T>` di `src/ERP.Application/Features/`:

| Modul | Commands DENGAN `AbstractValidator<T>` | Commands TANPA `AbstractValidator<T>` |
| :--- | :--- | :--- |
| **Master Data** | `CreateCompanyCommand`, `CreateBranchCommand`, `CreateProductCommand`, `CreateCustomerCommand`, `CreateSupplierCommand` | *(Lengkap 100%)* |
| **Inventory** | `CreateWarehouseCommand`, `CreateStockTransferCommand`, `CreateStockAdjustmentCommand` | `DispatchStockTransferCommand`, `ReceiveStockTransferCommand`, `ApproveStockAdjustmentCommand` |
| **Procurement** | `CreatePurchaseRequestCommand`, `CreatePurchaseOrderCommand`, `CreateGoodsReceiptCommand`, `CreateVendorBillCommand` | `SubmitPurchaseRequestCommand`, `ApprovePurchaseRequestCommand`, `RejectPurchaseRequestCommand`, `ApprovePurchaseOrderCommand`, `ReceiveGoodsReceiptCommand`, `PostVendorBillCommand` |
| **Sales** | `CreateQuotationCommand`, `CreateSalesOrderCommand`, `CreateDeliveryCommand`, `CreateSalesInvoiceCommand` | `SendQuotationCommand`, `AcceptQuotationCommand`, `ConfirmSalesOrderCommand`, `DispatchDeliveryCommand`, `PostSalesInvoiceCommand` |
| **Finance** | `CreateChartOfAccountCommand`, `CreateJournalEntryCommand`, `CreatePaymentCommand`, `CreateFiscalPeriodCommand` | `PostJournalEntryCommand`, `ReverseJournalEntryCommand`, `PostPaymentCommand`, `CloseFiscalPeriodCommand` |
| **HR & Payroll** | `CreateEmployeeCommand` | ❌ `RecordAttendanceCommand`, `CreateLeaveRequestCommand`, `ApproveLeaveRequestCommand`, `CreatePayrollPeriodCommand`, `CalculatePayrollCommand`, `ApprovePayrollCommand`, `PayPayrollCommand` |

**Analisis Celah Validasi:**
- Pada modul **HR**, hanya `CreateEmployeeCommand` yang memiliki `CreateEmployeeCommandValidator`. Command penting seperti `CreateLeaveRequestCommand` (`DaysCount`, `StartDate`, `EndDate`, `Reason`), `RecordAttendanceCommand` (`LateMinutes`), dan `CreatePayrollPeriodCommand` (`PeriodName`, `StartDate <= EndDate`) belum memiliki kelas `AbstractValidator<T>` di pipeline FluentValidation (meskipun sebagian divalidasi di metode domain `Create()`).
- Pada `CreateJournalEntryCommand.cs:71-86`, metode `JournalEntry.Create(...)` melempar `DomainException` jika `Description` kosong, dan `JournalEntry.AddLine(...)` melempar `DomainException` jika `debit > 0 && credit > 0`. Apabila validator tidak menangkap kondisi tersebut lebih dulu, `DomainException` akan keluar dari handler tanpa ditangkap oleh `ValidationPipelineBehavior`, menyebabkan *unhandled exception* di UI.

---

### 3.3 Penanganan `DomainException` dan `DbUpdateConcurrencyException` di Pipeline (`API-MED-02`)
- **Lokasi File:** `src/ERP.Application/DependencyInjection.cs` & `src/ERP.Domain/Common/BaseEntity.cs:73`
- **Akar Masalah:**
  - Metode domain di `ERP.Domain` melempar `DomainException` ketika invariant bisnis dilanggar (misal: stok tidak cukup di `StockBalance.ReserveStock`, atau pembalikan jurnal yang belum `Posted` di `JournalEntry.CreateReversal`).
  - Sebagian Command Handler membungkus pemanggilan metode domain dengan `try { ... } catch (Exception ex) { return Result.Failure(...); }`, namun tidak ada **`UnhandledExceptionPipelineBehavior` / `DomainExceptionPipelineBehavior`** terpusat di MediatR yang menangkap `DomainException` dan `DbUpdateConcurrencyException` secara konsisten lalu mengonversinya menjadi `Result.Failure(Error.Conflict(...))`!
- **Rekomendasi Perbaikan Teknis:**
  - Tambahkan `ExceptionHandlingPipelineBehavior<TRequest, TResponse>` sebagai behavior terluar di MediatR yang:
    1. Menangkap `DomainException` -> mengembalikan `Result.Failure(Error.Conflict("Domain.RuleViolation", ex.Message))`.
    2. Menangkap `DbUpdateConcurrencyException` -> mencatat peringatan konkurensi dan mengembalikan `Result.Failure(Error.Conflict("Database.ConcurrencyConflict", "Data telah diubah oleh pengguna lain. Silakan muat ulang halaman."))`.
    3. Menangkap `Exception` tak terduga -> mencatat `LogError` terstruktur via Serilog dan mengembalikan pesan aman tanpa membocorkan *stack trace* internal ke UI.
