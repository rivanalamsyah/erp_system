# Coding Standards & Architecture Conventions — Enterprise ERP System

> **Document Version:** 1.0.0  
> **Target Framework:** .NET 10 LTS | C# 14  
> **Related Documents:** [SOFTWARE_ARCHITECTURE.md](./SOFTWARE_ARCHITECTURE.md) | [PROJECT_STRUCTURE.md](./PROJECT_STRUCTURE.md) | [TESTING_STRATEGY.md](./TESTING_STRATEGY.md)

---

## 1. Konvensi Bahasa C# 14 & .NET 10 LTS

Seluruh kode yang ditulis dalam repositori ini wajib mematuhi standar modern C# 14:

1. **File-Scoped Namespaces:**  
   Gunakan file-scoped namespace untuk mengurangi indentasi:
   ```csharp
   namespace ERP.Application.Features.Sales.Commands.CreateSalesOrder;
   ```
2. **Primary Constructors:**  
   Gunakan primary constructor untuk dependency injection pada services dan handlers:
   ```csharp
   public class CreateSalesOrderCommandHandler(
       IApplicationDbContext context,
       ICurrentUserService currentUser,
       IDateTimeProvider dateTimeProvider)
       : IRequestHandler<CreateSalesOrderCommand, Result<Guid>>
   {
       // Fields terinjeksi secara otomatis
   }
   ```
3. **Collection Expressions:**  
   Gunakan sintaks collection expression `[...]` alih-alih `new List<T>()` atau `new T[]`:
   ```csharp
   private readonly List<string> _allowedStatuses = ["Draft", "PendingApproval", "Approved"];
   ```
4. **Pattern Matching & Switch Expressions:**  
   Gunakan ekspresi switch modern untuk evaluasi logika status:
   ```csharp
   public static string GetStatusColor(string status) => status switch
   {
       "Approved" or "Posted" => "Success",
       "PendingApproval" => "Warning",
       "Rejected" or "Cancelled" => "Error",
       _ => "Default"
   };
   ```

---

## 2. Aturan Pemrograman Asinkron (Async/Await & Cancellation)

1. **Propagasi `CancellationToken` Secara Menyeluruh:**  
   Setiap method asinkron wajib menerima dan meneruskan parameter `CancellationToken ct` hingga ke pemanggilan database atau I/O:
   ```csharp
   public async Task<Result<SalesOrderDto>> Handle(
       GetSalesOrderByIdQuery request, 
       CancellationToken cancellationToken)
   {
       var order = await context.SalesOrders
           .AsNoTracking()
           .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
           
       return order is not null 
           ? Result<SalesOrderDto>.Success(order.ToDto())
           : Result<SalesOrderDto>.Failure(Error.NotFound("Order.NotFound", "Order tidak ditemukan."));
   }
   ```
2. **Dilarang Keras Sinkronisasi Blocking (`.Result` / `.Wait()`):**  
   Penggunaan `.Result`, `.Wait()`, atau `.GetAwaiter().GetResult()` dilarang keras karena dapat memicu *thread starvation* dan *deadlock* pada sirkuit Blazor Server.

---

## 3. Best Practices Entity Framework Core 10 & Npgsql

### 3.1 Operasi Pembacaan (Query Best Practices)
1. **Wajib Menggunakan `AsNoTracking()` untuk Query:**  
   Seluruh query yang hanya bertujuan menampilkan data ke antarmuka wajib menggunakan `.AsNoTracking()` untuk meniadakan overhead Change Tracker di memori server:
   ```csharp
   var items = await context.Products
       .AsNoTracking()
       .Where(x => x.IsActive)
       .Select(x => new ProductListDto(x.Id, x.Sku, x.Name, x.SellingPrice))
       .ToListAsync(cancellationToken);
   ```
2. **Hindari Masalah N+1 (Explicit Projection / Split Queries):**  
   - Utamakan proyeksi langsung ke DTO menggunakan `.Select()`.
   - Jika memuat navigasi multi-koleksi bertingkat, gunakan `.AsSplitQuery()` untuk mencegah *Cartesian Explosion*:
   ```csharp
   var order = await context.SalesOrders
       .AsSplitQuery()
       .Include(x => x.Items)
       .FirstOrDefaultAsync(x => x.Id == id, ct);
   ```

### 3.2 Operasi Penulisan (Command Best Practices)
1. **Manipulasi Melalui Metode Domain:**  
   Dilarang memodifikasi properti entitas domain secara bebas dari luar. Seluruh mutasi status harus melalui method publik pada Domain Entity yang memvalidasi invarian bisnis:
   ```csharp
   // BENAR:
   order.ConfirmOrder(currentUser.UserId, dateTimeProvider.UtcNow);

   // SALAH:
   order.Status = "Confirmed";
   ```
2. **Bulk Update / Bulk Delete:**  
   Untuk pengubahan massal tanpa memuat ratusan entitas ke memori, gunakan `ExecuteUpdateAsync` atau `ExecuteDeleteAsync`:
   ```csharp
   await context.StockBalances
       .Where(x => x.WarehouseId == targetWarehouseId)
       .ExecuteUpdateAsync(s => s.SetProperty(b => b.LastUpdatedAtUtc, DateTime.UtcNow), ct);
   ```

---

## 4. Pola Organisasi Fitur (Vertical Slice / Feature Folders)

Di lapisan `ERP.Application`, file diorganisasikan per fitur bisnis (*Feature Folders*), bukan dikelompokkan berdasarkan tipe file:

```
ERP.Application/Features/Sales/
│
├── Commands/
│   ├── CreateSalesOrder/
│   │   ├── CreateSalesOrderCommand.cs
│   │   ├── CreateSalesOrderCommandHandler.cs
│   │   ├── CreateSalesOrderCommandValidator.cs
│   │   └── CreateSalesOrderDto.cs
│   └── ConfirmSalesOrder/
│       ├── ConfirmSalesOrderCommand.cs
│       └── ConfirmSalesOrderCommandHandler.cs
│
└── Queries/
    ├── GetSalesOrderById/
    │   ├── GetSalesOrderByIdQuery.cs
    │   ├── GetSalesOrderByIdQueryHandler.cs
    │   └── SalesOrderDetailDto.cs
    └── GetSalesOrdersPaged/
        ├── GetSalesOrdersPagedQuery.cs
        └── GetSalesOrdersPagedQueryHandler.cs
```

Keuntungan: Seluruh konteks logika dari suatu fitur berada dalam satu direktori yang kohesif, memudahkan pemeliharaan dan audit fitur.

---

## 5. Standar Komponen Blazor Interactive Server

1. **Pemisahan Tampilan dan Logika (Code-Behind Pattern):**  
   File Razor dipisahkan menjadi file markup (`.razor`) dan file C# partial class (`.razor.cs`):
   - `SalesOrderList.razor` (Struktur komponen MudBlazor & HTML)
   - `SalesOrderList.razor.cs` (Event handlers, state injection, pemanggilan MediatR)
2. **Manajemen Lifecycle & Pembersihan Sirkuit (`IAsyncDisposable`):**  
   Komponen yang berlangganan event SignalR atau timer background wajib mengimplementasikan `IAsyncDisposable` untuk melepaskan langganan saat komponen dibongkar:
   ```csharp
   public partial class SalesOrderPage : ComponentBase, IAsyncDisposable
   {
       [Inject] private ISnackbar Snackbar { get; set; } = default!;
       [Inject] private IMediator Mediator { get; set; } = default!;
       
       public async ValueTask DisposeAsync()
       {
           // Lepaskan event subscriptions dan batalkan pending cancellation token
           GC.SuppressFinalize(this);
           await ValueTask.CompletedTask;
       }
   }
   ```
3. **Penyimpanan State Aman:**  
   Dilarang keras menyimpan data rahasia (koneksi database, password hash) di static variable atau singleton service yang dapat bocor ke sirkuit pengguna lain. Seluruh state pengguna dikelola via *Scoped Services*.

