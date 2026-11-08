# Blueprint ERP System — .NET 10 + Blazor + PostgreSQL



Architecture recommendation

Modular Monolith



## 1. Technology stack final

Berdasarkan konfigurasi yang dipilih, berikut stack yang saya rekomendasikan.

| Komponen            | Teknologi                     |
| ------------------- | ----------------------------- |
| Framework           | .NET 10 LTS                   |
| Language            | C# 14                         |
| Frontend            | Blazor Web App                |
| Interactive Mode    | Interactive Server            |
| Backend             | ASP.NET Core                  |
| Database            | PostgreSQL                    |
| ORM                 | Entity Framework Core 10      |
| PostgreSQL Provider | Npgsql EF Core 10             |
| Authentication      | ASP.NET Core Identity         |
| Authorization       | Role-Based + Permission-Based |
| Validation          | FluentValidation              |
| Logging             | Serilog                       |
| API Documentation   | OpenAPI / Swagger             |
| Background Jobs     | Hangfire                      |
| Caching             | Redis (opsional)              |
| UI Components       | MudBlazor                     |
| Testing             | xUnit                         |
| Containerization    | Docker                        |
| Version Control     | Git                           |

Mengapa .NET 10? Per 8 November 2026, .NET 10 merupakan versi LTS aktif dengan dukungan hingga November 2028. .NET 11 masih dalam tahap Release Candidate, sehingga belum saya rekomendasikan untuk produksi.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://dotnet.microsoft.com\&sz=32)

.NET+2



Npgsql 10.0.3 tersedia untuk .NET 10 dan EF Core 10.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://www.nuget.org\&sz=32)

Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3+1



Mengapa Blazor Interactive Server?

- Seluruh business logic tetap berada di server.
- Tidak perlu membangun frontend dan backend menggunakan dua bahasa berbeda.
- Cocok untuk dashboard dan aplikasi internal perusahaan.
- Lebih mudah mengontrol otorisasi dan akses database.

Blazor mendukung render mode Interactive Server, WebAssembly, dan Auto. Untuk ERP internal, Interactive Server merupakan pilihan awal yang praktis.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://learn.microsoft.com\&sz=32)

Microsoft Learn+1



## 2. Modul bisnis ERP

Saya membagi sistem menjadi 9 modul utama.

| Modul                 | Submodul                                                    |
| --------------------- | ----------------------------------------------------------- |
| Dashboard             | Overview, statistik, aktivitas terbaru                      |
| Master Data           | Employee, department, position, customer, supplier, product |
| Human Resource        | Employee, attendance, leave, payroll                        |
| Inventory             | Product, category, warehouse, stock, stock movement         |
| Procurement           | Supplier, purchase request, purchase order, goods receipt   |
| Sales                 | Customer, quotation, sales order, delivery, sales invoice   |
| Finance & Accounting  | Cash/bank, expense, payment, journal, chart of accounts     |
| Reports               | Sales, purchase, inventory, HR, finance                     |
| System Administration | Users, roles, permissions, settings, audit logs             |

### Hubungan antarmodul

MASTER DATA

Employee · Customer · Supplier · Product

SALES

Quotation → Order

PROCUREMENT

Request → Purchase

INVENTORY

Stock · Warehouse · Movement

FINANCE & ACCOUNTING

Payment · Cash Flow · Journal · Ledger

REPORTING & ANALYTICS

Business Performance · Financial Reports

HR Management dan System Administration mendukung seluruh modul.

Contohnya, ketika Sales membuat pesanan, sistem dapat memeriksa ketersediaan stok. Ketika Procurement menerima barang, stok bertambah. Ketika transaksi penjualan dibayar, sistem mencatat pembayaran dan jurnal akuntansi.

## 3. Arsitektur aplikasi

Gunakan Clean Architecture dengan pendekatan Modular Monolith.

Artinya, aplikasi tetap satu deployment utama, tetapi kode dibagi berdasarkan tanggung jawab.

Presentation Layer

## ERP.Web

Blazor · Components · Pages · Layout · UI

Application Layer

## ERP.Application

Use Cases · Services · DTOs · Validation · Interfaces

Domain Layer

## ERP.Domain

Entities · Enums · Business Rules · Domain Events

Infrastructure Layer

## ERP.Infrastructure

EF Core · PostgreSQL · Identity · Repository · External Services

PostgreSQL Database

Persistent business data

Aturan dependensi:

- Domain tidak bergantung pada project lain.
- Application bergantung pada Domain.
- Infrastructure mengimplementasikan interface dari Application.
- Web menjadi composition root yang menghubungkan semuanya.

Jangan meletakkan business logic langsung di file `.razor`. Komponen Blazor sebaiknya hanya menangani tampilan dan interaksi pengguna.

## 4. Struktur folder dan solution

Saya merekomendasikan satu solution bernama `EnterpriseERP`.

```
EnterpriseERP/
│
├── src/
│   │
│   ├── ERP.Web/
│   │   ├── Components/
│   │   │   ├── Layout/
│   │   │   ├── Shared/
│   │   │   ├── Dashboard/
│   │   │   ├── MasterData/
│   │   │   ├── HumanResources/
│   │   │   ├── Inventory/
│   │   │   ├── Procurement/
│   │   │   ├── Sales/
│   │   │   ├── Finance/
│   │   │   └── Reports/
│   │   ├── Services/
│   │   ├── Middleware/
│   │   ├── wwwroot/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── ERP.Domain/
│   │   ├── Common/
│   │   ├── Entities/
│   │   │   ├── MasterData/
│   │   │   ├── HR/
│   │   │   ├── Inventory/
│   │   │   ├── Procurement/
│   │   │   ├── Sales/
│   │   │   └── Finance/
│   │   ├── Enums/
│   │   ├── Events/
│   │   └── Exceptions/
│   │
│   ├── ERP.Application/
│   │   ├── Common/
│   │   ├── Interfaces/
│   │   ├── Features/
│   │   │   ├── Employees/
│   │   │   ├── Customers/
│   │   │   ├── Products/
│   │   │   ├── Inventory/
│   │   │   ├── Procurement/
│   │   │   ├── Sales/
│   │   │   └── Finance/
│   │   ├── DTOs/
│   │   ├── Validators/
│   │   └── Behaviors/
│   │
│   ├── ERP.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── Context/
│   │   │   ├── Configurations/
│   │   │   ├── Migrations/
│   │   │   └── Seeders/
│   │   ├── Identity/
│   │   ├── Repositories/
│   │   ├── Services/
│   │   └── BackgroundJobs/
│   │
│   └── ERP.Shared/
│       ├── Constants/
│       ├── Models/
│       └── Helpers/
│
├── tests/
│   ├── ERP.Domain.Tests/
│   ├── ERP.Application.Tests/
│   ├── ERP.Infrastructure.Tests/
│   └── ERP.Web.Tests/
│
├── docs/
│   ├── architecture/
│   ├── database/
│   └── api/
│
├── docker-compose.yml
├── EnterpriseERP.sln
└── README.md
```

Catatan: Setiap folder `Features` sebaiknya menggunakan struktur per fitur, misalnya `Create`, `Update`, `GetList`, dan `GetById`, bukan menggabungkan semua service dalam satu file besar.

## 5. Database schema PostgreSQL

Untuk ERP general, saya menyarankan database menggunakan pemisahan schema PostgreSQL berdasarkan domain bisnis.

Contohnya:

```
enterprise_erp
│
├── identity
├── master
├── hr
├── inventory
├── procurement
├── sales
├── finance
└── audit
```

Dengan demikian, tabel setiap modul tidak bercampur secara logis.

### A. Identity & Access Management

| Tabel            | Fungsi                     |
| ---------------- | -------------------------- |
| users            | Akun pengguna              |
| roles            | Daftar role                |
| permissions      | Daftar permission          |
| user_roles       | Relasi user dan role       |
| role_permissions | Relasi role dan permission |
| user_sessions    | Informasi sesi login       |

Gunakan ASP.NET Core Identity sebagai fondasi autentikasi, kemudian tambahkan permission khusus ERP.

### B. Master Data

| Tabel       | Kolom penting                                                  |
| ----------- | -------------------------------------------------------------- |
| companies   | id, code, name, tax_number                                     |
| branches    | id, company_id, code, name                                     |
| departments | id, branch_id, name                                            |
| positions   | id, name                                                       |
| employees   | id, employee_number, department_id, position_id                |
| customers   | id, code, name, email, phone                                   |
| suppliers   | id, code, name, email, phone                                   |
| categories  | id, name                                                       |
| units       | id, name, abbreviation                                         |
| products    | id, sku, category_id, unit_id, name, cost_price, selling_price |

### C. Human Resource

| Tabel           | Kolom penting                                                            |
| --------------- | ------------------------------------------------------------------------ |
| attendance      | employee_id, attendance_date, check_in, check_out                        |
| leave_types     | id, name                                                                 |
| leave_requests  | employee_id, leave_type_id, start_date, end_date, status                 |
| payroll_periods | id, period_month, period_year, status                                    |
| payrolls        | employee_id, period_id, basic_salary, allowances, deductions, net_salary |

### D. Inventory

| Tabel                  | Kolom penting                                                   |
| ---------------------- | --------------------------------------------------------------- |
| warehouses             | id, code, name, location                                        |
| stock_balances         | product_id, warehouse_id, quantity                              |
| stock_movements        | product_id, warehouse_id, movement_type, quantity, reference_id |
| stock_adjustments      | id, warehouse_id, adjustment_date, status                       |
| stock_adjustment_items | adjustment_id, product_id, quantity_before, quantity_after      |
| stock_transfers        | id, source_warehouse_id, destination_warehouse_id, status       |
| stock_transfer_items   | transfer_id, product_id, quantity                               |

### E. Procurement

| Tabel                  | Kolom penting                                       |
| ---------------------- | --------------------------------------------------- |
| purchase_requests      | id, request_number, requested_by, status            |
| purchase_request_items | request_id, product_id, quantity                    |
| purchase_orders        | id, po_number, supplier_id, order_date, status      |
| purchase_order_items   | purchase_order_id, product_id, quantity, unit_price |
| goods_receipts         | id, purchase_order_id, received_date, status        |
| goods_receipt_items    | receipt_id, product_id, quantity_received           |

### F. Sales

| Tabel               | Kolom penting                                                  |
| ------------------- | -------------------------------------------------------------- |
| quotations          | id, quotation_number, customer_id, date, status                |
| quotation_items     | quotation_id, product_id, quantity, unit_price                 |
| sales_orders        | id, order_number, customer_id, order_date, status              |
| sales_order_items   | sales_order_id, product_id, quantity, unit_price               |
| deliveries          | id, sales_order_id, delivery_date, status                      |
| delivery_items      | delivery_id, product_id, quantity                              |
| sales_invoices      | id, invoice_number, customer_id, invoice_date, due_date, total |
| sales_invoice_items | invoice_id, product_id, quantity, unit_price                   |

### G. Finance & Accounting

| Tabel               | Kolom penting                                            |
| ------------------- | -------------------------------------------------------- |
| chart_of_accounts   | id, account_code, account_name, account_type             |
| fiscal_periods      | id, period_name, start_date, end_date, status            |
| journal_entries     | id, journal_number, journal_date, description, status    |
| journal_lines       | journal_id, account_id, debit, credit                    |
| payments            | id, payment_number, payment_date, payment_method, amount |
| payment_allocations | payment_id, invoice_id, allocated_amount                 |
| expenses            | id, expense_number, category, amount, date               |
| bank_accounts       | id, bank_name, account_number                            |
| cash_transactions   | id, transaction_type, amount, reference                  |

### H. Audit & System

| Tabel                | Fungsi                     |
| -------------------- | -------------------------- |
| audit_logs           | Riwayat perubahan data     |
| notifications        | Notifikasi pengguna        |
| system_settings      | Konfigurasi aplikasi       |
| document_attachments | Metadata dokumen           |
| approval_requests    | Permintaan persetujuan     |
| approval_histories   | Riwayat keputusan approval |

### 5.1 Entity relationship diagram

Berikut gambaran relasi inti database:

\#chatgpt-mermaid-\_r_hj\_{font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";font-size:16px;fill:rgb(237, 237, 237);}@keyframes edge-animation-frame{from{stroke-dashoffset:0;}}@keyframes dash{to{stroke-dashoffset:0;}}#chatgpt-mermaid-\_r_hj\_ .edge-animation-slow{stroke-dasharray:9,5!important;stroke-dashoffset:900;animation:dash 50s linear infinite;stroke-linecap:round;}#chatgpt-mermaid-\_r_hj\_ .edge-animation-fast{stroke-dasharray:9,5!important;stroke-dashoffset:900;animation:dash 20s linear infinite;stroke-linecap:round;}#chatgpt-mermaid-\_r_hj\_ .error-icon{fill:rgb(48, 48, 48);}#chatgpt-mermaid-\_r_hj\_ .error-text{fill:rgb(237, 237, 237);stroke:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_hj\_ .edge-thickness-normal{stroke-width:1px;}#chatgpt-mermaid-\_r_hj\_ .edge-thickness-thick{stroke-width:3.5px;}#chatgpt-mermaid-\_r_hj\_ .edge-pattern-solid{stroke-dasharray:0;}#chatgpt-mermaid-\_r_hj\_ .edge-thickness-invisible{stroke-width:0;fill:none;}#chatgpt-mermaid-\_r_hj\_ .edge-pattern-dashed{stroke-dasharray:3;}#chatgpt-mermaid-\_r_hj\_ .edge-pattern-dotted{stroke-dasharray:2;}#chatgpt-mermaid-\_r_hj\_ .marker{fill:rgb(175, 175, 175);stroke:rgb(175, 175, 175);}#chatgpt-mermaid-\_r_hj\_ .marker.cross{stroke:rgb(175, 175, 175);}#chatgpt-mermaid-\_r_hj\_ svg{font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";font-size:16px;}#chatgpt-mermaid-\_r_hj\_ p{margin:0;}#chatgpt-mermaid-\_r_hj\_ .entityBox{fill:rgb(9, 23, 44);stroke:rgb(31, 78, 148);}#chatgpt-mermaid-\_r_hj\_ .relationshipLabelBox{fill:rgb(48, 48, 48);opacity:0.7;background-color:rgb(48, 48, 48);}#chatgpt-mermaid-\_r_hj\_ .relationshipLabelBox rect{opacity:0.5;}#chatgpt-mermaid-\_r_hj\_ .labelBkg{background-color:rgba(48, 48, 48, 0.5);}#chatgpt-mermaid-\_r_hj\_ .edgeLabel{background-color:rgb(0, 0, 0);}#chatgpt-mermaid-\_r_hj\_ .edgeLabel .label rect{fill:rgb(0, 0, 0);}#chatgpt-mermaid-\_r_hj\_ .edgeLabel .label text{fill:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_hj\_ .edgeLabel .label{fill:rgb(31, 78, 148);font-size:14px;}#chatgpt-mermaid-\_r_hj\_ .label{font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";color:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_hj\_ .edge-pattern-dashed{stroke-dasharray:8,8;}#chatgpt-mermaid-\_r_hj\_ .node rect,#chatgpt-mermaid-\_r_hj\_ .node circle,#chatgpt-mermaid-\_r_hj\_ .node ellipse,#chatgpt-mermaid-\_r_hj\_ .node polygon{fill:rgb(9, 23, 44);stroke:rgb(31, 78, 148);stroke-width:1px;}#chatgpt-mermaid-\_r_hj\_ .relationshipLine{stroke:rgb(175, 175, 175);stroke-width:1px;fill:none;}#chatgpt-mermaid-\_r_hj\_ .marker{fill:none!important;stroke:rgb(175, 175, 175)!important;stroke-width:1;}#chatgpt-mermaid-\_r_hj\_ [data-look=neo].labelBkg{background-color:rgba(48, 48, 48, 0.5);}#chatgpt-mermaid-\_r_hj\_ .node .neo-node{stroke:rgb(31, 78, 148);}#chatgpt-mermaid-\_r_hj\_ [data-look="neo"].node rect,#chatgpt-mermaid-\_r_hj\_ [data-look="neo"].cluster rect,#chatgpt-mermaid-\_r_hj\_ [data-look="neo"].node polygon{stroke:url(#chatgpt-mermaid-\_r_hj\_-gradient);filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_hj\_ [data-look="neo"].swimlane.cluster rect{filter:none;}#chatgpt-mermaid-\_r_hj\_ [data-look="neo"].node path{stroke:url(#chatgpt-mermaid-\_r_hj\_-gradient);stroke-width:1px;}#chatgpt-mermaid-\_r_hj\_ [data-look="neo"].node .outer-path{filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_hj\_ [data-look="neo"].node .neo-line path{stroke:rgb(31, 78, 148);filter:none;}#chatgpt-mermaid-\_r_hj\_ [data-look="neo"].node circle{stroke:url(#chatgpt-mermaid-\_r_hj\_-gradient);filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_hj\_ [data-look="neo"].node circle .state-start{fill:#000000;}#chatgpt-mermaid-\_r_hj\_ [data-look="neo"].icon-shape .icon{fill:url(#chatgpt-mermaid-\_r_hj\_-gradient);filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_hj\_ [data-look="neo"].icon-shape .icon-neo path{stroke:url(#chatgpt-mermaid-\_r_hj\_-gradient);filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_hj\_ :root{--mermaid-font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";}COMPANIESBRANCHESDEPARTMENTSEMPLOYEESPOSITIONSCUSTOMERSSALES_ORDERSSALES_ORDER_ITEMSSALES_INVOICESPAYMENT_ALLOCATIONSPAYMENTSSUPPLIERSPURCHASE_ORDERSPURCHASE_ORDER_ITEMSGOODS_RECEIPTSPRODUCTSSTOCK_BALANCESWAREHOUSESATTENDANCELEAVE_REQUESTSPAYROLLSJOURNAL_ENTRIESJOURNAL_LINESCHART_OF_ACCOUNTShascontainsemploysassignsplacescontainsgeneratesreceivesallocatesreceivescontainsfulfilled_bysoldpurchasedstoredholdsrecordsrequestsreceivescontainsreferences

Catatan desain database penting:

1. Gunakan UUID (`uuid`) untuk primary key.
2. Gunakan `numeric(18,2)` untuk nominal uang, bukan floating point.
3. Gunakan `timestamptz` untuk timestamp.
4. Semua transaksi penting memiliki nomor dokumen unik.
5. Gunakan foreign key dan unique constraints.
6. Transaksi yang sudah diposting ke accounting tidak boleh dihapus sembarangan.
7. Stock balance harus konsisten dengan stock movements.
8. Gunakan `created_at`, `updated_at`, `created_by`, dan `updated_by` pada tabel bisnis.

Untuk ERP general yang nantinya mendukung banyak perusahaan, `company_id` sebaiknya menjadi bagian dari seluruh data bisnis yang relevan. Ini mempersiapkan dukungan multi-company tanpa harus membangun ulang database.

## 6. Business workflow utama

Modul ERP harus saling terhubung melalui workflow.

### Sales workflow

\#chatgpt-mermaid-\_r_hq\_{font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";font-size:16px;fill:rgb(237, 237, 237);}@keyframes edge-animation-frame{from{stroke-dashoffset:0;}}@keyframes dash{to{stroke-dashoffset:0;}}#chatgpt-mermaid-\_r_hq\_ .edge-animation-slow{stroke-dasharray:9,5!important;stroke-dashoffset:900;animation:dash 50s linear infinite;stroke-linecap:round;}#chatgpt-mermaid-\_r_hq\_ .edge-animation-fast{stroke-dasharray:9,5!important;stroke-dashoffset:900;animation:dash 20s linear infinite;stroke-linecap:round;}#chatgpt-mermaid-\_r_hq\_ .error-icon{fill:rgb(48, 48, 48);}#chatgpt-mermaid-\_r_hq\_ .error-text{fill:rgb(237, 237, 237);stroke:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_hq\_ .edge-thickness-normal{stroke-width:1px;}#chatgpt-mermaid-\_r_hq\_ .edge-thickness-thick{stroke-width:3.5px;}#chatgpt-mermaid-\_r_hq\_ .edge-pattern-solid{stroke-dasharray:0;}#chatgpt-mermaid-\_r_hq\_ .edge-thickness-invisible{stroke-width:0;fill:none;}#chatgpt-mermaid-\_r_hq\_ .edge-pattern-dashed{stroke-dasharray:3;}#chatgpt-mermaid-\_r_hq\_ .edge-pattern-dotted{stroke-dasharray:2;}#chatgpt-mermaid-\_r_hq\_ .marker{fill:rgb(175, 175, 175);stroke:rgb(175, 175, 175);}#chatgpt-mermaid-\_r_hq\_ .marker.cross{stroke:rgb(175, 175, 175);}#chatgpt-mermaid-\_r_hq\_ svg{font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";font-size:16px;}#chatgpt-mermaid-\_r_hq\_ p{margin:0;}#chatgpt-mermaid-\_r_hq\_ .label{font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";color:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_hq\_ .cluster-label text{fill:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_hq\_ .cluster-label span{color:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_hq\_ .cluster-label span p{background-color:transparent;}#chatgpt-mermaid-\_r_hq\_ .label text,#chatgpt-mermaid-\_r_hq\_ span{fill:rgb(237, 237, 237);color:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_hq\_ .node rect,#chatgpt-mermaid-\_r_hq\_ .node circle,#chatgpt-mermaid-\_r_hq\_ .node ellipse,#chatgpt-mermaid-\_r_hq\_ .node polygon,#chatgpt-mermaid-\_r_hq\_ .node path{fill:rgb(9, 23, 44);stroke:rgb(31, 78, 148);stroke-width:1px;}#chatgpt-mermaid-\_r_hq\_ .rough-node .label text,#chatgpt-mermaid-\_r_hq\_ .node .label text,#chatgpt-mermaid-\_r_hq\_ .image-shape .label,#chatgpt-mermaid-\_r_hq\_ .icon-shape .label{text-anchor:middle;}#chatgpt-mermaid-\_r_hq\_ .node .katex path{fill:#000;stroke:#000;stroke-width:1px;}#chatgpt-mermaid-\_r_hq\_ .rough-node .label,#chatgpt-mermaid-\_r_hq\_ .node .label,#chatgpt-mermaid-\_r_hq\_ .image-shape .label,#chatgpt-mermaid-\_r_hq\_ .icon-shape .label{text-align:center;}#chatgpt-mermaid-\_r_hq\_ .node.clickable{cursor:pointer;}#chatgpt-mermaid-\_r_hq\_ .root .anchor path{fill:rgb(175, 175, 175)!important;stroke-width:0;stroke:rgb(175, 175, 175);}#chatgpt-mermaid-\_r_hq\_ .arrowheadPath{fill:rgb(175, 175, 175);}#chatgpt-mermaid-\_r_hq\_ .edgePath .path{stroke:rgb(175, 175, 175);stroke-width:1px;}#chatgpt-mermaid-\_r_hq\_ .flowchart-link{stroke:rgb(175, 175, 175);fill:none;}#chatgpt-mermaid-\_r_hq\_ .edgeLabel{background-color:rgb(0, 0, 0);text-align:center;}#chatgpt-mermaid-\_r_hq\_ .edgeLabel p{background-color:rgb(0, 0, 0);}#chatgpt-mermaid-\_r_hq\_ .edgeLabel rect{opacity:0.5;background-color:rgb(0, 0, 0);fill:rgb(0, 0, 0);}#chatgpt-mermaid-\_r_hq\_ .labelBkg{background-color:rgba(0, 0, 0, 0.5);}#chatgpt-mermaid-\_r_hq\_ .cluster rect{fill:rgb(48, 48, 48);stroke:rgba(255, 255, 255, 0.15);stroke-width:1px;}#chatgpt-mermaid-\_r_hq\_ .cluster text{fill:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_hq\_ .cluster span{color:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_hq\_ div.mermaidTooltip{position:absolute;text-align:center;max-width:200px;padding:2px;font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";font-size:12px;background:rgb(48, 48, 48);border:1px solid rgba(255, 255, 255, 0.15);border-radius:2px;pointer-events:none;z-index:100;}#chatgpt-mermaid-\_r_hq\_ .flowchartTitleText{text-anchor:middle;font-size:18px;fill:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_hq\_ rect.text{fill:none;stroke-width:0;}#chatgpt-mermaid-\_r_hq\_ .icon-shape,#chatgpt-mermaid-\_r_hq\_ .image-shape{background-color:rgb(0, 0, 0);text-align:center;}#chatgpt-mermaid-\_r_hq\_ .icon-shape p,#chatgpt-mermaid-\_r_hq\_ .image-shape p{background-color:rgb(0, 0, 0);padding:2px;}#chatgpt-mermaid-\_r_hq\_ .icon-shape .label rect,#chatgpt-mermaid-\_r_hq\_ .image-shape .label rect{opacity:0.5;background-color:rgb(0, 0, 0);fill:rgb(0, 0, 0);}#chatgpt-mermaid-\_r_hq\_ .label-icon{display:inline-block;height:1em;overflow:visible;vertical-align:-0.125em;}#chatgpt-mermaid-\_r_hq\_ .node .label-icon path{fill:currentColor;stroke:revert;stroke-width:revert;}#chatgpt-mermaid-\_r_hq\_ .node .neo-node{stroke:rgb(31, 78, 148);}#chatgpt-mermaid-\_r_hq\_ [data-look="neo"].node rect,#chatgpt-mermaid-\_r_hq\_ [data-look="neo"].cluster rect,#chatgpt-mermaid-\_r_hq\_ [data-look="neo"].node polygon{stroke:url(#chatgpt-mermaid-\_r_hq\_-gradient);filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_hq\_ [data-look="neo"].swimlane.cluster rect{filter:none;}#chatgpt-mermaid-\_r_hq\_ [data-look="neo"].node path{stroke:url(#chatgpt-mermaid-\_r_hq\_-gradient);stroke-width:1px;}#chatgpt-mermaid-\_r_hq\_ [data-look="neo"].node .outer-path{filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_hq\_ [data-look="neo"].node .neo-line path{stroke:rgb(31, 78, 148);filter:none;}#chatgpt-mermaid-\_r_hq\_ [data-look="neo"].node circle{stroke:url(#chatgpt-mermaid-\_r_hq\_-gradient);filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_hq\_ [data-look="neo"].node circle .state-start{fill:#000000;}#chatgpt-mermaid-\_r_hq\_ [data-look="neo"].icon-shape .icon{fill:url(#chatgpt-mermaid-\_r_hq\_-gradient);filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_hq\_ [data-look="neo"].icon-shape .icon-neo path{stroke:url(#chatgpt-mermaid-\_r_hq\_-gradient);filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_hq\_ .node text{font-size:14px;font-weight:600;letter-spacing:normal;fill:rgb(153, 206, 255);}#chatgpt-mermaid-\_r_hq\_ .edgeLabels text{font-size:13px;font-weight:600;letter-spacing:-0.08px;fill:rgb(153, 206, 255);}#chatgpt-mermaid-\_r_hq\_ .node tspan[font-weight="normal"],#chatgpt-mermaid-\_r_hq\_ .edgeLabels tspan[font-weight="normal"]{font-weight:600;}#chatgpt-mermaid-\_r_hq\_ .edgeLabel .label rect{opacity:1;rx:13px;ry:13px;fill:rgb(0, 14, 26);stroke:rgb(26, 62, 95);stroke-width:1px;}#chatgpt-mermaid-\_r_hq\_ .node rect,#chatgpt-mermaid-\_r_hq\_ .node circle,#chatgpt-mermaid-\_r_hq\_ .node ellipse,#chatgpt-mermaid-\_r_hq\_ .node polygon,#chatgpt-mermaid-\_r_hq\_ .node path{fill:rgb(0, 40, 77);stroke:rgba(255, 255, 255, 0.1);stroke-width:1px;}#chatgpt-mermaid-\_r_hq\_ .node rect{rx:16px;ry:16px;}#chatgpt-mermaid-\_r_hq\_ .mermaid-decision .label-container{fill:rgb(0, 14, 26);stroke:rgb(26, 62, 95);stroke-dasharray:2,2;}#chatgpt-mermaid-\_r_hq\_ .edgePaths .flowchart-link{stroke:rgb(175, 175, 175);stroke-width:1px;stroke-linecap:round;stroke-linejoin:round;}#chatgpt-mermaid-\_r_hq\_ .marker{fill:rgb(175, 175, 175);stroke:rgb(175, 175, 175);}#chatgpt-mermaid-\_r_hq\_ :root{--mermaid-font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";}CustomerQuotationQuotation Approved?Sales OrderCheck InventoryStock Available?DeliveryProcurementGoods ReceiptSales InvoicePaymentAccounting JournalCompletedNoYesYesNo

### Procurement workflow

\#chatgpt-mermaid-\_r_i1\_{font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";font-size:16px;fill:rgb(237, 237, 237);}@keyframes edge-animation-frame{from{stroke-dashoffset:0;}}@keyframes dash{to{stroke-dashoffset:0;}}#chatgpt-mermaid-\_r_i1\_ .edge-animation-slow{stroke-dasharray:9,5!important;stroke-dashoffset:900;animation:dash 50s linear infinite;stroke-linecap:round;}#chatgpt-mermaid-\_r_i1\_ .edge-animation-fast{stroke-dasharray:9,5!important;stroke-dashoffset:900;animation:dash 20s linear infinite;stroke-linecap:round;}#chatgpt-mermaid-\_r_i1\_ .error-icon{fill:rgb(48, 48, 48);}#chatgpt-mermaid-\_r_i1\_ .error-text{fill:rgb(237, 237, 237);stroke:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_i1\_ .edge-thickness-normal{stroke-width:1px;}#chatgpt-mermaid-\_r_i1\_ .edge-thickness-thick{stroke-width:3.5px;}#chatgpt-mermaid-\_r_i1\_ .edge-pattern-solid{stroke-dasharray:0;}#chatgpt-mermaid-\_r_i1\_ .edge-thickness-invisible{stroke-width:0;fill:none;}#chatgpt-mermaid-\_r_i1\_ .edge-pattern-dashed{stroke-dasharray:3;}#chatgpt-mermaid-\_r_i1\_ .edge-pattern-dotted{stroke-dasharray:2;}#chatgpt-mermaid-\_r_i1\_ .marker{fill:rgb(175, 175, 175);stroke:rgb(175, 175, 175);}#chatgpt-mermaid-\_r_i1\_ .marker.cross{stroke:rgb(175, 175, 175);}#chatgpt-mermaid-\_r_i1\_ svg{font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";font-size:16px;}#chatgpt-mermaid-\_r_i1\_ p{margin:0;}#chatgpt-mermaid-\_r_i1\_ .label{font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";color:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_i1\_ .cluster-label text{fill:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_i1\_ .cluster-label span{color:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_i1\_ .cluster-label span p{background-color:transparent;}#chatgpt-mermaid-\_r_i1\_ .label text,#chatgpt-mermaid-\_r_i1\_ span{fill:rgb(237, 237, 237);color:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_i1\_ .node rect,#chatgpt-mermaid-\_r_i1\_ .node circle,#chatgpt-mermaid-\_r_i1\_ .node ellipse,#chatgpt-mermaid-\_r_i1\_ .node polygon,#chatgpt-mermaid-\_r_i1\_ .node path{fill:rgb(9, 23, 44);stroke:rgb(31, 78, 148);stroke-width:1px;}#chatgpt-mermaid-\_r_i1\_ .rough-node .label text,#chatgpt-mermaid-\_r_i1\_ .node .label text,#chatgpt-mermaid-\_r_i1\_ .image-shape .label,#chatgpt-mermaid-\_r_i1\_ .icon-shape .label{text-anchor:middle;}#chatgpt-mermaid-\_r_i1\_ .node .katex path{fill:#000;stroke:#000;stroke-width:1px;}#chatgpt-mermaid-\_r_i1\_ .rough-node .label,#chatgpt-mermaid-\_r_i1\_ .node .label,#chatgpt-mermaid-\_r_i1\_ .image-shape .label,#chatgpt-mermaid-\_r_i1\_ .icon-shape .label{text-align:center;}#chatgpt-mermaid-\_r_i1\_ .node.clickable{cursor:pointer;}#chatgpt-mermaid-\_r_i1\_ .root .anchor path{fill:rgb(175, 175, 175)!important;stroke-width:0;stroke:rgb(175, 175, 175);}#chatgpt-mermaid-\_r_i1\_ .arrowheadPath{fill:rgb(175, 175, 175);}#chatgpt-mermaid-\_r_i1\_ .edgePath .path{stroke:rgb(175, 175, 175);stroke-width:1px;}#chatgpt-mermaid-\_r_i1\_ .flowchart-link{stroke:rgb(175, 175, 175);fill:none;}#chatgpt-mermaid-\_r_i1\_ .edgeLabel{background-color:rgb(0, 0, 0);text-align:center;}#chatgpt-mermaid-\_r_i1\_ .edgeLabel p{background-color:rgb(0, 0, 0);}#chatgpt-mermaid-\_r_i1\_ .edgeLabel rect{opacity:0.5;background-color:rgb(0, 0, 0);fill:rgb(0, 0, 0);}#chatgpt-mermaid-\_r_i1\_ .labelBkg{background-color:rgba(0, 0, 0, 0.5);}#chatgpt-mermaid-\_r_i1\_ .cluster rect{fill:rgb(48, 48, 48);stroke:rgba(255, 255, 255, 0.15);stroke-width:1px;}#chatgpt-mermaid-\_r_i1\_ .cluster text{fill:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_i1\_ .cluster span{color:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_i1\_ div.mermaidTooltip{position:absolute;text-align:center;max-width:200px;padding:2px;font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";font-size:12px;background:rgb(48, 48, 48);border:1px solid rgba(255, 255, 255, 0.15);border-radius:2px;pointer-events:none;z-index:100;}#chatgpt-mermaid-\_r_i1\_ .flowchartTitleText{text-anchor:middle;font-size:18px;fill:rgb(237, 237, 237);}#chatgpt-mermaid-\_r_i1\_ rect.text{fill:none;stroke-width:0;}#chatgpt-mermaid-\_r_i1\_ .icon-shape,#chatgpt-mermaid-\_r_i1\_ .image-shape{background-color:rgb(0, 0, 0);text-align:center;}#chatgpt-mermaid-\_r_i1\_ .icon-shape p,#chatgpt-mermaid-\_r_i1\_ .image-shape p{background-color:rgb(0, 0, 0);padding:2px;}#chatgpt-mermaid-\_r_i1\_ .icon-shape .label rect,#chatgpt-mermaid-\_r_i1\_ .image-shape .label rect{opacity:0.5;background-color:rgb(0, 0, 0);fill:rgb(0, 0, 0);}#chatgpt-mermaid-\_r_i1\_ .label-icon{display:inline-block;height:1em;overflow:visible;vertical-align:-0.125em;}#chatgpt-mermaid-\_r_i1\_ .node .label-icon path{fill:currentColor;stroke:revert;stroke-width:revert;}#chatgpt-mermaid-\_r_i1\_ .node .neo-node{stroke:rgb(31, 78, 148);}#chatgpt-mermaid-\_r_i1\_ [data-look="neo"].node rect,#chatgpt-mermaid-\_r_i1\_ [data-look="neo"].cluster rect,#chatgpt-mermaid-\_r_i1\_ [data-look="neo"].node polygon{stroke:url(#chatgpt-mermaid-\_r_i1\_-gradient);filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_i1\_ [data-look="neo"].swimlane.cluster rect{filter:none;}#chatgpt-mermaid-\_r_i1\_ [data-look="neo"].node path{stroke:url(#chatgpt-mermaid-\_r_i1\_-gradient);stroke-width:1px;}#chatgpt-mermaid-\_r_i1\_ [data-look="neo"].node .outer-path{filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_i1\_ [data-look="neo"].node .neo-line path{stroke:rgb(31, 78, 148);filter:none;}#chatgpt-mermaid-\_r_i1\_ [data-look="neo"].node circle{stroke:url(#chatgpt-mermaid-\_r_i1\_-gradient);filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_i1\_ [data-look="neo"].node circle .state-start{fill:#000000;}#chatgpt-mermaid-\_r_i1\_ [data-look="neo"].icon-shape .icon{fill:url(#chatgpt-mermaid-\_r_i1\_-gradient);filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_i1\_ [data-look="neo"].icon-shape .icon-neo path{stroke:url(#chatgpt-mermaid-\_r_i1\_-gradient);filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}#chatgpt-mermaid-\_r_i1\_ .node text{font-size:14px;font-weight:600;letter-spacing:normal;fill:rgb(153, 206, 255);}#chatgpt-mermaid-\_r_i1\_ .edgeLabels text{font-size:13px;font-weight:600;letter-spacing:-0.08px;fill:rgb(153, 206, 255);}#chatgpt-mermaid-\_r_i1\_ .node tspan[font-weight="normal"],#chatgpt-mermaid-\_r_i1\_ .edgeLabels tspan[font-weight="normal"]{font-weight:600;}#chatgpt-mermaid-\_r_i1\_ .edgeLabel .label rect{opacity:1;rx:13px;ry:13px;fill:rgb(0, 14, 26);stroke:rgb(26, 62, 95);stroke-width:1px;}#chatgpt-mermaid-\_r_i1\_ .node rect,#chatgpt-mermaid-\_r_i1\_ .node circle,#chatgpt-mermaid-\_r_i1\_ .node ellipse,#chatgpt-mermaid-\_r_i1\_ .node polygon,#chatgpt-mermaid-\_r_i1\_ .node path{fill:rgb(0, 40, 77);stroke:rgba(255, 255, 255, 0.1);stroke-width:1px;}#chatgpt-mermaid-\_r_i1\_ .node rect{rx:16px;ry:16px;}#chatgpt-mermaid-\_r_i1\_ .mermaid-decision .label-container{fill:rgb(0, 14, 26);stroke:rgb(26, 62, 95);stroke-dasharray:2,2;}#chatgpt-mermaid-\_r_i1\_ .edgePaths .flowchart-link{stroke:rgb(175, 175, 175);stroke-width:1px;stroke-linecap:round;stroke-linejoin:round;}#chatgpt-mermaid-\_r_i1\_ .marker{fill:rgb(175, 175, 175);stroke:rgb(175, 175, 175);}#chatgpt-mermaid-\_r_i1\_ :root{--mermaid-font-family:-apple-system-body,ui-sans-serif,-apple-system,system-ui,"Segoe UI",Helvetica,"Apple Color Emoji",Arial,sans-serif,"Segoe UI Emoji","Segoe UI Symbol";}Purchase RequestApprovalApproved?RejectedPurchase OrderSupplierGoods ReceiptUpdate InventorySupplier InvoicePaymentAccounting JournalNoYes

### Finance workflow

Setiap transaksi yang berdampak pada keuangan harus dapat menghasilkan jurnal sesuai aturan akuntansi.

Contoh:

| Transaksi            | Debit         | Kredit        |
| -------------------- | ------------- | ------------- |
| Penjualan kredit     | Piutang usaha | Pendapatan    |
| Pembayaran customer  | Kas/Bank      | Piutang usaha |
| Pembelian persediaan | Persediaan    | Utang usaha   |
| Pembayaran supplier  | Utang usaha   | Kas/Bank      |
| Biaya operasional    | Beban         | Kas/Bank      |

Perlu diperhatikan bahwa contoh tersebut merupakan ilustrasi dasar. Pajak, HPP, retur, diskon, dan metode pengakuan persediaan harus diperhitungkan dalam implementasi akuntansi yang sebenarnya.

## 7. Struktur akses pengguna

ERP harus memiliki permission yang granular, bukan hanya menyembunyikan menu berdasarkan role.

Contoh role:

| Role              | Akses                                      |
| ----------------- | ------------------------------------------ |
| Super Admin       | Seluruh sistem                             |
| Director          | Dashboard dan laporan perusahaan           |
| HR Manager        | HR dan karyawan                            |
| Finance Manager   | Finance dan accounting                     |
| Sales Manager     | Sales dan customer                         |
| Procurement Staff | Procurement                                |
| Warehouse Staff   | Inventory                                  |
| Employee          | Data dan layanan karyawan sesuai hak akses |

Contoh permission:

```
sales.view
sales.create
sales.update
sales.delete
sales.approve

inventory.view
inventory.adjust
inventory.transfer

finance.view
finance.create
finance.post
finance.void

hr.employee.view
hr.employee.manage
hr.payroll.manage
```

Tambahkan juga pembatasan berdasarkan company dan branch, sehingga pengguna cabang A tidak otomatis bisa membaca transaksi cabang B.

## 8. Standar keamanan dan kualitas

Untuk ERP yang digunakan perusahaan, saya merekomendasikan:

- ASP.NET Core Identity untuk autentikasi.
- Password hashing bawaan Identity.
- MFA untuk akun administrator.
- Authorization policy pada setiap operasi.
- Server-side validation.
- Audit log untuk transaksi sensitif.
- Database backup otomatis.
- Soft delete hanya untuk data yang memang boleh diarsipkan.
- Database transaction untuk proses multi-tabel.
- Concurrency control untuk menghindari perubahan data bersamaan.
- HTTPS dan secure cookie.
- Proteksi CSRF sesuai pola autentikasi.
- Rate limiting pada endpoint yang relevan.
- Pengujian unit dan integrasi.
- Secret disimpan melalui environment variables atau secret manager.

Untuk Blazor Interactive Server, pemeriksaan izin wajib dilakukan di server, bukan hanya di UI.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://learn.microsoft.com\&sz=32)

Microsoft Learn+1



## 9. Roadmap implementasi

Untuk membangun seluruh modul dengan kualitas production-ready, saya menyarankan roadmap sekitar 12–16 minggu. Durasi aktual bergantung pada jumlah developer dan kompleksitas setiap modul.

| Fase              | Pekerjaan                                    | Output                    |
| ----------------- | -------------------------------------------- | ------------------------- |
| 1. Foundation     | Solution, arsitektur, konfigurasi PostgreSQL | Project siap dikembangkan |
| 2. Authentication | Login, Identity, RBAC, permission            | Sistem akses pengguna     |
| 3. Master Data    | Employee, customer, supplier, product        | Data utama perusahaan     |
| 4. HR Management  | Employee, attendance, leave, payroll         | Modul HR                  |
| 5. Inventory      | Warehouse, stock, adjustment, transfer       | Modul inventaris          |
| 6. Procurement    | Purchase request, PO, goods receipt          | Modul pembelian           |
| 7. Sales          | Quotation, sales order, invoice              | Modul penjualan           |
| 8. Finance        | Payment, journal, accounting                 | Modul keuangan            |
| 9. Reporting      | Dashboard, laporan, export                   | Modul pelaporan           |
| 10. Finalization  | Testing, security, Docker, deployment        | ERP siap produksi         |

### Prioritas implementasi

Jangan membangun seluruh halaman sekaligus. Pastikan setiap modul sudah memiliki:

- CRUD lengkap.
- Search, filter, pagination.
- Detail data.
- Validasi input.
- Approval workflow jika diperlukan.
- Audit trail.
- Role dan permission.
- Integrasi dengan modul terkait.
- Unit dan integration testing.

## 10. Setup awal project

Berikut contoh perintah untuk memulai project menggunakan .NET CLI.

Step 1 — Buat solution

```
mkdir EnterpriseERP
cd EnterpriseERP

dotnet new sln -n EnterpriseERP

mkdir src
mkdir tests
```

Step 2 — Buat project utama

```
dotnet new blazor -n ERP.Web -o src/ERP.Web

dotnet new classlib -n ERP.Domain -o src/ERP.Domain

dotnet new classlib -n ERP.Application -o src/ERP.Application

dotnet new classlib -n ERP.Infrastructure -o src/ERP.Infrastructure

dotnet new classlib -n ERP.Shared -o src/ERP.Shared
```

Step 3 — Tambahkan project ke solution

```
dotnet sln add src/ERP.Web
dotnet sln add src/ERP.Domain
dotnet sln add src/ERP.Application
dotnet sln add src/ERP.Infrastructure
dotnet sln add src/ERP.Shared
```

Step 4 — Atur project references

```
dotnet add src/ERP.Application reference src/ERP.Domain

dotnet add src/ERP.Infrastructure reference src/ERP.Domain

dotnet add src/ERP.Infrastructure reference src/ERP.Application

dotnet add src/ERP.Web reference src/ERP.Application

dotnet add src/ERP.Web reference src/ERP.Infrastructure
```

Step 5 — Install PostgreSQL provider

```
dotnet add src/ERP.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3
```

Tambahkan juga EF Core Design dengan versi 10.x yang sesuai.

```
dotnet add src/ERP.Infrastructure package Microsoft.EntityFrameworkCore.Design --version 10.0.4

dotnet tool install --global dotnet-ef --version 10.0.4
```

Pastikan versi EF Core dan provider Npgsql kompatibel sebelum melakukan restore.

Step 6 — Jalankan project

```
dotnet restore
dotnet build
dotnet run --project src/ERP.Web
```

Untuk autentikasi, konfigurasi ASP.NET Core Identity dan cookie authentication. Untuk ERP internal, gunakan autentikasi server-side yang terintegrasi dengan Blazor, bukan menyimpan token sensitif di browser.

Dokumentasi teknis:

- [Microsoft Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/?utm_source=chatgpt.com)
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?utm_source=chatgpt.com)
- [Npgsql EF Core](https://www.npgsql.org/efcore/?utm_source=chatgpt.com)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/?utm_source=chatgpt.com)

## 11. Standar hasil akhir yang harus dicapai

Saya menyarankan menetapkan kriteria berikut sejak awal:

## ERP Production Readiness Checklist

0 dari 13 terpenuhi

0%

Seluruh modul memiliki CRUD yang berfungsi

Database memiliki relasi dan constraint yang benar

Role dan permission berjalan di server

Seluruh transaksi memiliki validasi

Sales dan procurement terintegrasi dengan inventory

Transaksi keuangan menghasilkan jurnal yang seimbang

Dashboard menggunakan data database aktual

Audit log mencatat aktivitas penting

Tersedia export PDF dan Excel

Responsive untuk desktop, tablet, dan mobile

Automated testing berjalan

Backup dan recovery database tersedia

Deployment menggunakan HTTPS dan Docker

## 12. Kesimpulan arsitektur

# Enterprise ERP

Final recommended stack

Framework\
.NET 10 LTS

Frontend\
Blazor Web App

Interactive Mode\
Interactive Server

Database\
PostgreSQL

ORM\
EF Core 10

Architecture\
Clean Architecture

Application Pattern\
Modular Monolith

UI Components\
MudBlazor

Deployment\
Docker + Linux VPS

Saran langkah berikutnya: Sebelum membuat halaman dashboard atau CRUD, selesaikan dahulu fondasi project, struktur solution, konfigurasi database, Identity, dan authorization. Ini akan menghindari banyak perubahan arsitektur ketika modul ERP mulai bertambah.