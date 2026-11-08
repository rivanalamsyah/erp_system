# Entity Relationship Diagram (ERD) — Enterprise ERP System

> **Document Version:** 1.0.0  
> **Target Database Engine:** PostgreSQL 16+  
> **Related Documents:** [DATABASE_DESIGN.md](./DATABASE_DESIGN.md) | [DATABASE_SCHEMA.md](./DATABASE_SCHEMA.md) | [DATA_DICTIONARY.md](./DATA_DICTIONARY.md)

---

## 1. Diagram Relasi Entitas Inti Global (Global Enterprise ERD)

Diagram berikut merepresentasikan relasi relasional lintas domain sistem ERP, mencakup struktur korporasi, rantai pasok, penjualan, persediaan, penggajian, dan akuntansi berpasangan (*double-entry*):

```mermaid
erDiagram
    COMPANIES ||--o{ BRANCHES : "operates"
    COMPANIES ||--o{ DEPARTMENTS : "contains"
    COMPANIES ||--o{ PRODUCTS : "owns"
    COMPANIES ||--o{ CUSTOMERS : "serves"
    COMPANIES ||--o{ SUPPLIERS : "purchases_from"
    COMPANIES ||--o{ CHART_OF_ACCOUNTS : "maintains"

    BRANCHES ||--o{ WAREHOUSES : "controls"
    BRANCHES ||--o{ EMPLOYEES : "employs"
    BRANCHES ||--o{ SALES_ORDERS : "issues"
    BRANCHES ||--o{ PURCHASE_ORDERS : "issues"

    USERS ||--o{ USER_ROLES : "assigned"
    ROLES ||--o{ USER_ROLES : "groups"
    ROLES ||--o{ ROLE_PERMISSIONS : "contains"
    PERMISSIONS ||--o{ ROLE_PERMISSIONS : "grants"
    USERS ||--o{ USER_BRANCHES : "scoped_to"
    BRANCHES ||--o{ USER_BRANCHES : "accessible_by"

    DEPARTMENTS ||--o{ EMPLOYEES : "places"
    EMPLOYEES ||--o{ ATTENDANCE_RECORDS : "records"
    EMPLOYEES ||--o{ LEAVE_REQUESTS : "requests"
    EMPLOYEES ||--o{ PAYROLL_ITEMS : "receives"
    PAYROLL_PERIODS ||--o{ PAYROLL_ITEMS : "aggregates"

    PRODUCT_CATEGORIES ||--o{ PRODUCTS : "classifies"
    UNITS_OF_MEASURE ||--o{ PRODUCTS : "quantifies"
    PRODUCTS ||--o{ STOCK_BALANCES : "stocked_in"
    WAREHOUSES ||--o{ STOCK_BALANCES : "stores"
    PRODUCTS ||--o{ STOCK_MOVEMENTS : "tracks"
    WAREHOUSES ||--o{ STOCK_MOVEMENTS : "originates_from"

    CUSTOMERS ||--o{ QUOTATIONS : "requests"
    QUOTATIONS ||--o{ QUOTATION_ITEMS : "details"
    QUOTATIONS ||--o| SALES_ORDERS : "converted_to"
    CUSTOMERS ||--o{ SALES_ORDERS : "places"
    SALES_ORDERS ||--o{ SALES_ORDER_ITEMS : "contains"
    SALES_ORDERS ||--o{ DELIVERIES : "fulfilled_by"
    DELIVERIES ||--o{ DELIVERY_ITEMS : "ships"
    SALES_ORDERS ||--o{ SALES_INVOICES : "billed_by"

    SUPPLIERS ||--o{ PURCHASE_ORDERS : "supplies"
    PURCHASE_REQUESTS ||--o{ PURCHASE_REQUEST_ITEMS : "details"
    PURCHASE_REQUESTS ||--o| PURCHASE_ORDERS : "converts_to"
    PURCHASE_ORDERS ||--o{ PURCHASE_ORDER_ITEMS : "contains"
    PURCHASE_ORDERS ||--o{ GOODS_RECEIPTS : "received_by"
    GOODS_RECEIPTS ||--o{ GOODS_RECEIPT_ITEMS : "inspects"
    PURCHASE_ORDERS ||--o{ VENDOR_BILLS : "billed_by"

    SALES_INVOICES ||--o{ PAYMENT_ALLOCATIONS : "settled_by"
    VENDOR_BILLS ||--o{ PAYMENT_ALLOCATIONS : "settled_by"
    PAYMENTS ||--o{ PAYMENT_ALLOCATIONS : "allocates"
    CASH_BANK_ACCOUNTS ||--o{ PAYMENTS : "funds"

    CHART_OF_ACCOUNTS ||--o{ JOURNAL_LINES : "categorizes"
    JOURNAL_ENTRIES ||--o{ JOURNAL_LINES : "contains"
```

---

## 2. Diagram Domain Relasional Spesifik

### 2.1 Domain Identitas & Keamanan (`identity`)

```mermaid
erDiagram
    USERS {
        uuid id PK
        varchar username UK
        varchar email UK
        text password_hash
        varchar full_name
        boolean is_active
        boolean two_factor_enabled
    }

    ROLES {
        uuid id PK
        varchar name UK
        text description
        boolean is_system_role
    }

    PERMISSIONS {
        uuid id PK
        varchar code UK
        varchar module
        text description
    }

    USER_ROLES {
        uuid user_id PK,FK
        uuid role_id PK,FK
        timestamptz assigned_at_utc
    }

    ROLE_PERMISSIONS {
        uuid role_id PK,FK
        uuid permission_id PK,FK
        timestamptz granted_at_utc
    }

    USER_BRANCHES {
        uuid user_id PK,FK
        uuid branch_id PK,FK
        boolean is_default
    }

    USERS ||--o{ USER_ROLES : has
    ROLES ||--o{ USER_ROLES : assigned_to
    ROLES ||--o{ ROLE_PERMISSIONS : includes
    PERMISSIONS ||--o{ ROLE_PERMISSIONS : mapped_in
    USERS ||--o{ USER_BRANCHES : accesses
```

---

### 2.2 Domain Rantai Pasok Penjualan (Sales: O2C)

```mermaid
erDiagram
    CUSTOMERS ||--o{ QUOTATIONS : requests
    QUOTATIONS ||--o{ QUOTATION_ITEMS : specifies
    QUOTATIONS ||--o| SALES_ORDERS : generates
    CUSTOMERS ||--o{ SALES_ORDERS : orders
    SALES_ORDERS ||--o{ SALES_ORDER_ITEMS : contains
    SALES_ORDERS ||--o{ DELIVERIES : dispatches
    DELIVERIES ||--o{ DELIVERY_ITEMS : packages
    SALES_ORDERS ||--o{ SALES_INVOICES : bills
    
    QUOTATIONS {
        uuid id PK
        varchar quotation_number UK
        uuid customer_id FK
        date expiration_date
        varchar status
        numeric total_amount
    }

    SALES_ORDERS {
        uuid id PK
        varchar order_number UK
        uuid customer_id FK
        uuid quotation_id FK
        date order_date
        varchar status
        numeric total_amount
    }

    DELIVERIES {
        uuid id PK
        varchar delivery_number UK
        uuid sales_order_id FK
        uuid warehouse_id FK
        date delivery_date
        varchar status
    }

    SALES_INVOICES {
        uuid id PK
        varchar invoice_number UK
        uuid sales_order_id FK
        uuid customer_id FK
        date due_date
        numeric total_amount
        numeric paid_amount
        varchar status
    }
```

---

### 2.3 Domain Pengadaan & Pembelian (Procurement: P2P)

```mermaid
erDiagram
    SUPPLIERS ||--o{ PURCHASE_ORDERS : provides
    PURCHASE_REQUESTS ||--o{ PURCHASE_REQUEST_ITEMS : requires
    PURCHASE_ORDERS ||--o{ PURCHASE_ORDER_ITEMS : contains
    PURCHASE_ORDERS ||--o{ GOODS_RECEIPTS : receives
    GOODS_RECEIPTS ||--o{ GOODS_RECEIPT_ITEMS : verifies
    PURCHASE_ORDERS ||--o{ VENDOR_BILLS : charges
    
    PURCHASE_REQUESTS {
        uuid id PK
        varchar request_number UK
        date required_date
        varchar status
    }

    PURCHASE_ORDERS {
        uuid id PK
        varchar po_number UK
        uuid supplier_id FK
        date order_date
        varchar status
        numeric total_amount
    }

    GOODS_RECEIPTS {
        uuid id PK
        varchar receipt_number UK
        uuid purchase_order_id FK
        uuid warehouse_id FK
        date receipt_date
        varchar status
    }

    VENDOR_BILLS {
        uuid id PK
        varchar bill_number UK
        uuid purchase_order_id FK
        uuid supplier_id FK
        date due_date
        numeric total_amount
        numeric paid_amount
        varchar status
    }
```

---

### 2.4 Domain Persediaan & Logistik Gudang (`inventory`)

```mermaid
erDiagram
    WAREHOUSES ||--o{ STOCK_BALANCES : holds
    PRODUCTS ||--o{ STOCK_BALANCES : tracked_in
    WAREHOUSES ||--o{ STOCK_MOVEMENTS : logs
    PRODUCTS ||--o{ STOCK_MOVEMENTS : mutates

    STOCK_BALANCES {
        uuid id PK
        uuid warehouse_id FK
        uuid product_id FK
        numeric physical_quantity
        numeric reserved_quantity
        numeric average_cost
        timestamptz last_updated_at_utc
    }

    STOCK_MOVEMENTS {
        uuid id PK
        uuid warehouse_id FK
        uuid product_id FK
        varchar movement_type
        numeric quantity
        varchar direction
        numeric unit_cost
        numeric total_cost
        varchar reference_number
        timestamptz movement_date_utc
    }
```

---

### 2.5 Domain Akuntansi & Buku Besar (`finance`)

```mermaid
erDiagram
    CHART_OF_ACCOUNTS ||--o{ JOURNAL_LINES : posts_to
    JOURNAL_ENTRIES ||--o{ JOURNAL_LINES : itemizes
    CASH_BANK_ACCOUNTS ||--o{ PAYMENTS : executes
    PAYMENTS ||--o{ PAYMENT_ALLOCATIONS : splits

    JOURNAL_ENTRIES {
        uuid id PK
        varchar journal_number UK
        date journal_date
        varchar reference_type
        varchar reference_number
        varchar status
        numeric total_debit
        numeric total_credit
    }

    JOURNAL_LINES {
        uuid id PK
        uuid journal_entry_id FK
        uuid account_id FK
        numeric debit
        numeric credit
        text description
    }

    CHART_OF_ACCOUNTS {
        uuid id PK
        varchar account_code UK
        varchar account_name
        varchar account_type
        varchar normal_balance
        boolean is_header
    }
```

