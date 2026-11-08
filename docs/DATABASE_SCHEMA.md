# Database Schema Specifications (DDL) — Enterprise ERP System

> **Document Version:** 1.0.0  
> **Target Database Engine:** PostgreSQL 16+  
> **Related Documents:** [DATABASE_DESIGN.md](./DATABASE_DESIGN.md) | [ENTITY_RELATIONSHIP_DIAGRAM.md](./ENTITY_RELATIONSHIP_DIAGRAM.md) | [DATA_DICTIONARY.md](./DATA_DICTIONARY.md)

---

## 1. Skrip Inisialisasi Schema PostgreSQL

```sql
-- 1. Inisialisasi Ekstensi & 8 Schema Domain Terisolasi
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS master;
CREATE SCHEMA IF NOT EXISTS hr;
CREATE SCHEMA IF NOT EXISTS inventory;
CREATE SCHEMA IF NOT EXISTS procurement;
CREATE SCHEMA IF NOT EXISTS sales;
CREATE SCHEMA IF NOT EXISTS finance;
CREATE SCHEMA IF NOT EXISTS audit;
```

---

## 2. Definisi Schema: `identity` (Autentikasi & Otorisasi)

```sql
-- 2.1 Tabel Pengguna
CREATE TABLE identity.users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    username varchar(50) NOT NULL UNIQUE,
    email varchar(100) NOT NULL UNIQUE,
    password_hash text NOT NULL,
    security_stamp text NOT NULL,
    full_name varchar(100) NOT NULL,
    phone_number varchar(20),
    is_active boolean NOT NULL DEFAULT TRUE,
    two_factor_enabled boolean NOT NULL DEFAULT FALSE,
    lockout_end_utc timestamptz,
    access_failed_count integer NOT NULL DEFAULT 0,
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc timestamptz,
    is_deleted boolean NOT NULL DEFAULT FALSE
);

-- 2.2 Tabel Peran (Roles)
CREATE TABLE identity.roles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(50) NOT NULL UNIQUE,
    description text,
    is_system_role boolean NOT NULL DEFAULT FALSE,
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 2.3 Tabel Izin Spesifik (Permissions)
CREATE TABLE identity.permissions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(100) NOT NULL UNIQUE, -- e.g., 'sales.order.approve'
    module varchar(50) NOT NULL,       -- e.g., 'Sales'
    description text,
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 2.4 Relasi User ke Role (Many-to-Many)
CREATE TABLE identity.user_roles (
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    role_id uuid NOT NULL REFERENCES identity.roles(id) ON DELETE CASCADE,
    assigned_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, role_id)
);

-- 2.5 Relasi Role ke Permission (Many-to-Many)
CREATE TABLE identity.role_permissions (
    role_id uuid NOT NULL REFERENCES identity.roles(id) ON DELETE CASCADE,
    permission_id uuid NOT NULL REFERENCES identity.permissions(id) ON DELETE CASCADE,
    granted_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (role_id, permission_id)
);

-- 2.6 Pemetaan Hak Akses Cabang Pengguna (Data Scoping)
CREATE TABLE identity.user_branches (
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    branch_id uuid NOT NULL, -- FK to master.branches(id) ditambahkan setelah tabel dibuat
    is_default boolean NOT NULL DEFAULT FALSE,
    PRIMARY KEY (user_id, branch_id)
);

-- 2.7 Sesi Pengguna & Pelacakan Login
CREATE TABLE identity.user_sessions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    ip_address varchar(45) NOT NULL,
    user_agent text,
    login_time_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_activity_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_expired boolean NOT NULL DEFAULT FALSE
);
```

---

## 3. Definisi Schema: `master` (Data Induk & Struktur Korporasi)

```sql
-- 3.1 Perusahaan Legal (Multi-Company)
CREATE TABLE master.companies (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(20) NOT NULL UNIQUE,
    name varchar(150) NOT NULL,
    tax_identification_number varchar(30) NOT NULL, -- NPWP
    address text NOT NULL,
    phone varchar(20),
    email varchar(100),
    currency_code varchar(3) NOT NULL DEFAULT 'IDR',
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    updated_at_utc timestamptz,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE
);

-- 3.2 Kantor Cabang Operasional (Multi-Branch)
CREATE TABLE master.branches (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    code varchar(20) NOT NULL,
    name varchar(100) NOT NULL,
    address text,
    phone varchar(20),
    is_head_office boolean NOT NULL DEFAULT FALSE,
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    updated_at_utc timestamptz,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_branches_company_code UNIQUE (company_id, code)
);

-- Tambahkan FK user_branches ke master.branches
ALTER TABLE identity.user_branches
    ADD CONSTRAINT fk_user_branches_branch FOREIGN KEY (branch_id)
    REFERENCES master.branches(id) ON DELETE CASCADE;

-- 3.3 Departemen & Divisi
CREATE TABLE master.departments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    code varchar(20) NOT NULL,
    name varchar(100) NOT NULL,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_departments_company_code UNIQUE (company_id, code)
);

-- 3.4 Satuan Ukuran (Units of Measure - UoM)
CREATE TABLE master.units_of_measure (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    code varchar(10) NOT NULL,
    name varchar(50) NOT NULL,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_uom_company_code UNIQUE (company_id, code)
);

-- 3.5 Kategori Produk Hierarkis
CREATE TABLE master.product_categories (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    parent_category_id uuid REFERENCES master.product_categories(id) ON DELETE RESTRICT,
    code varchar(20) NOT NULL,
    name varchar(100) NOT NULL,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_categories_company_code UNIQUE (company_id, code)
);

-- 3.6 Master Katalog Produk
CREATE TABLE master.products (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    category_id uuid NOT NULL REFERENCES master.product_categories(id) ON DELETE RESTRICT,
    uom_id uuid NOT NULL REFERENCES master.units_of_measure(id) ON DELETE RESTRICT,
    sku varchar(50) NOT NULL,
    barcode varchar(50),
    name varchar(200) NOT NULL,
    description text,
    product_type varchar(20) NOT NULL CHECK (product_type IN ('Inventory', 'Service', 'RawMaterial')),
    purchase_price numeric(18, 4) NOT NULL DEFAULT 0 CHECK (purchase_price >= 0),
    selling_price numeric(18, 4) NOT NULL DEFAULT 0 CHECK (selling_price >= 0),
    minimum_stock_level numeric(14, 4) NOT NULL DEFAULT 0 CHECK (minimum_stock_level >= 0),
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    updated_at_utc timestamptz,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_products_company_sku UNIQUE (company_id, sku)
);

-- 3.7 Master Pelanggan (Customer)
CREATE TABLE master.customers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    code varchar(30) NOT NULL,
    name varchar(150) NOT NULL,
    email varchar(100),
    phone varchar(20),
    billing_address text NOT NULL,
    shipping_address text,
    tax_number varchar(30),
    credit_limit numeric(18, 2) NOT NULL DEFAULT 0 CHECK (credit_limit >= 0),
    payment_terms_days integer NOT NULL DEFAULT 30 CHECK (payment_terms_days >= 0),
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    updated_at_utc timestamptz,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_customers_company_code UNIQUE (company_id, code)
);

-- 3.8 Master Pemasok (Supplier)
CREATE TABLE master.suppliers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    code varchar(30) NOT NULL,
    name varchar(150) NOT NULL,
    email varchar(100),
    phone varchar(20),
    address text NOT NULL,
    tax_number varchar(30),
    payment_terms_days integer NOT NULL DEFAULT 30 CHECK (payment_terms_days >= 0),
    bank_account_name varchar(100),
    bank_account_number varchar(50),
    bank_name varchar(50),
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    updated_at_utc timestamptz,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_suppliers_company_code UNIQUE (company_id, code)
);

-- 3.9 Generator Nomor Dokumen Sekuensial
CREATE TABLE master.document_sequences (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE CASCADE,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE CASCADE,
    document_type varchar(30) NOT NULL, -- e.g., 'SO', 'PO', 'INV', 'DO', 'JV'
    year integer NOT NULL,
    month integer NOT NULL,
    current_sequence integer NOT NULL DEFAULT 0,
    CONSTRAINT uq_doc_seq UNIQUE (company_id, branch_id, document_type, year, month)
);
```

---

## 4. Definisi Schema: `hr` (Kepegawaian & Penggajian)

```sql
-- 4.1 Master Karyawan
CREATE TABLE hr.employees (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    department_id uuid NOT NULL REFERENCES master.departments(id) ON DELETE RESTRICT,
    user_id uuid REFERENCES identity.users(id) ON DELETE SET NULL,
    employee_number varchar(30) NOT NULL,
    full_name varchar(100) NOT NULL,
    identity_card_number varchar(30) NOT NULL, -- KIK/KTP
    birth_date date NOT NULL,
    hire_date date NOT NULL,
    employment_status varchar(20) NOT NULL CHECK (employment_status IN ('Permanent', 'Contract', 'Probation')),
    basic_salary numeric(18, 2) NOT NULL DEFAULT 0 CHECK (basic_salary >= 0),
    bank_account_number varchar(50),
    bank_name varchar(50),
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    updated_at_utc timestamptz,
    updated_by_user_id uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_employees_company_number UNIQUE (company_id, employee_number)
);

-- 4.2 Catatan Presensi Harian
CREATE TABLE hr.attendance_records (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id uuid NOT NULL REFERENCES hr.employees(id) ON DELETE RESTRICT,
    attendance_date date NOT NULL,
    check_in_time timestamptz,
    check_out_time timestamptz,
    status varchar(20) NOT NULL CHECK (status IN ('Present', 'Late', 'Excused', 'Sick', 'Absent')),
    late_minutes integer NOT NULL DEFAULT 0,
    notes text,
    CONSTRAINT uq_emp_attendance_date UNIQUE (employee_id, attendance_date)
);

-- 4.3 Tipe & Saldo Permohonan Cuti
CREATE TABLE hr.leave_types (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    name varchar(50) NOT NULL,
    annual_allowance_days integer NOT NULL DEFAULT 12,
    is_deleted boolean NOT NULL DEFAULT FALSE
);

CREATE TABLE hr.leave_requests (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id uuid NOT NULL REFERENCES hr.employees(id) ON DELETE RESTRICT,
    leave_type_id uuid NOT NULL REFERENCES hr.leave_types(id) ON DELETE RESTRICT,
    start_date date NOT NULL,
    end_date date NOT NULL,
    days_count integer NOT NULL CHECK (days_count > 0),
    reason text NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('Pending', 'Approved', 'Rejected')),
    approved_by_user_id uuid REFERENCES identity.users(id),
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 4.4 Periode & Slip Penggajian (Payroll)
CREATE TABLE hr.payroll_periods (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    period_name varchar(50) NOT NULL,
    start_date date NOT NULL,
    end_date date NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'Calculated', 'Approved', 'Paid')),
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    approved_by_user_id uuid REFERENCES identity.users(id)
);

CREATE TABLE hr.payroll_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    period_id uuid NOT NULL REFERENCES hr.payroll_periods(id) ON DELETE CASCADE,
    employee_id uuid NOT NULL REFERENCES hr.employees(id) ON DELETE RESTRICT,
    basic_salary numeric(18, 2) NOT NULL DEFAULT 0,
    allowances numeric(18, 2) NOT NULL DEFAULT 0,
    deductions numeric(18, 2) NOT NULL DEFAULT 0,
    income_tax_pph21 numeric(18, 2) NOT NULL DEFAULT 0,
    net_salary numeric(18, 2) NOT NULL CHECK (net_salary >= 0),
    is_paid boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_payroll_period_employee UNIQUE (period_id, employee_id)
);
```

---

## 5. Definisi Schema: `inventory` (Pergudangan & Stok)

```sql
-- 5.1 Master Gudang
CREATE TABLE inventory.warehouses (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    code varchar(20) NOT NULL,
    name varchar(100) NOT NULL,
    location text,
    is_active boolean NOT NULL DEFAULT TRUE,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_warehouses_company_code UNIQUE (company_id, code)
);

-- 5.2 Saldo Stok Real-Time (Stock Balances)
CREATE TABLE inventory.stock_balances (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    warehouse_id uuid NOT NULL REFERENCES inventory.warehouses(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES master.products(id) ON DELETE RESTRICT,
    physical_quantity numeric(14, 4) NOT NULL DEFAULT 0,
    reserved_quantity numeric(14, 4) NOT NULL DEFAULT 0 CHECK (reserved_quantity >= 0),
    average_cost numeric(18, 4) NOT NULL DEFAULT 0 CHECK (average_cost >= 0),
    last_updated_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_warehouse_product UNIQUE (warehouse_id, product_id)
);

-- 5.3 Buku Besar Mutasi Stok Perpetual (Stock Movements) - IMMUTABLE
CREATE TABLE inventory.stock_movements (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    warehouse_id uuid NOT NULL REFERENCES inventory.warehouses(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES master.products(id) ON DELETE RESTRICT,
    movement_type varchar(30) NOT NULL CHECK (movement_type IN ('PurchaseReceipt', 'SalesDelivery', 'TransferIn', 'TransferOut', 'AdjustmentPositive', 'AdjustmentNegative')),
    quantity numeric(14, 4) NOT NULL CHECK (quantity > 0),
    direction varchar(3) NOT NULL CHECK (direction IN ('IN', 'OUT')),
    unit_cost numeric(18, 4) NOT NULL CHECK (unit_cost >= 0),
    total_cost numeric(18, 2) NOT NULL CHECK (total_cost >= 0),
    reference_number varchar(50) NOT NULL,
    movement_date_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL
);

-- 5.4 Transfer Antar Gudang
CREATE TABLE inventory.stock_transfers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    transfer_number varchar(50) NOT NULL UNIQUE,
    source_warehouse_id uuid NOT NULL REFERENCES inventory.warehouses(id) ON DELETE RESTRICT,
    destination_warehouse_id uuid NOT NULL REFERENCES inventory.warehouses(id) ON DELETE RESTRICT,
    transfer_date date NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'Dispatched', 'Received', 'Cancelled')),
    notes text,
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    updated_at_utc timestamptz,
    is_deleted boolean NOT NULL DEFAULT FALSE
);

CREATE TABLE inventory.stock_transfer_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transfer_id uuid NOT NULL REFERENCES inventory.stock_transfers(id) ON DELETE CASCADE,
    product_id uuid NOT NULL REFERENCES master.products(id) ON DELETE RESTRICT,
    quantity numeric(14, 4) NOT NULL CHECK (quantity > 0)
);

-- 5.5 Penyesuaian Stok (Stock Adjustment)
CREATE TABLE inventory.stock_adjustments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    warehouse_id uuid NOT NULL REFERENCES inventory.warehouses(id) ON DELETE RESTRICT,
    adjustment_number varchar(50) NOT NULL UNIQUE,
    adjustment_date date NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'Approved', 'Rejected')),
    reason text NOT NULL,
    approved_by_user_id uuid REFERENCES identity.users(id),
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    is_deleted boolean NOT NULL DEFAULT FALSE
);

CREATE TABLE inventory.stock_adjustment_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    adjustment_id uuid NOT NULL REFERENCES inventory.stock_adjustments(id) ON DELETE CASCADE,
    product_id uuid NOT NULL REFERENCES master.products(id) ON DELETE RESTRICT,
    system_quantity numeric(14, 4) NOT NULL,
    physical_quantity numeric(14, 4) NOT NULL,
    difference_quantity numeric(14, 4) NOT NULL,
    unit_cost numeric(18, 4) NOT NULL DEFAULT 0
);
```

---

## 6. Definisi Schema: `procurement` (Pengadaan & Pembelian)

```sql
-- 6.1 Permintaan Pembelian (Purchase Request - PR)
CREATE TABLE procurement.purchase_requests (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    request_number varchar(50) NOT NULL UNIQUE,
    request_date date NOT NULL,
    required_date date NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'PendingApproval', 'Approved', 'Rejected', 'Closed')),
    notes text,
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    approved_by_user_id uuid REFERENCES identity.users(id),
    is_deleted boolean NOT NULL DEFAULT FALSE
);

CREATE TABLE procurement.purchase_request_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id uuid NOT NULL REFERENCES procurement.purchase_requests(id) ON DELETE CASCADE,
    product_id uuid NOT NULL REFERENCES master.products(id) ON DELETE RESTRICT,
    quantity numeric(14, 4) NOT NULL CHECK (quantity > 0),
    estimated_unit_price numeric(18, 4) NOT NULL DEFAULT 0
);

-- 6.2 Pesanan Pembelian (Purchase Order - PO)
CREATE TABLE procurement.purchase_orders (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    supplier_id uuid NOT NULL REFERENCES master.suppliers(id) ON DELETE RESTRICT,
    po_number varchar(50) NOT NULL UNIQUE,
    order_date date NOT NULL,
    expected_delivery_date date,
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'PendingApproval', 'Approved', 'PartiallyReceived', 'FullyReceived', 'Billed', 'Closed', 'Cancelled')),
    subtotal_amount numeric(18, 2) NOT NULL DEFAULT 0 CHECK (subtotal_amount >= 0),
    tax_amount numeric(18, 2) NOT NULL DEFAULT 0 CHECK (tax_amount >= 0),
    total_amount numeric(18, 2) NOT NULL DEFAULT 0 CHECK (total_amount >= 0),
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    approved_by_user_id uuid REFERENCES identity.users(id),
    is_deleted boolean NOT NULL DEFAULT FALSE
);

CREATE TABLE procurement.purchase_order_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    purchase_order_id uuid NOT NULL REFERENCES procurement.purchase_orders(id) ON DELETE CASCADE,
    product_id uuid NOT NULL REFERENCES master.products(id) ON DELETE RESTRICT,
    quantity numeric(14, 4) NOT NULL CHECK (quantity > 0),
    received_quantity numeric(14, 4) NOT NULL DEFAULT 0 CHECK (received_quantity >= 0),
    unit_price numeric(18, 4) NOT NULL CHECK (unit_price >= 0),
    discount_rate numeric(5, 2) NOT NULL DEFAULT 0,
    line_total numeric(18, 2) NOT NULL CHECK (line_total >= 0)
);

-- 6.3 Penerimaan Barang (Goods Receipt Note - GRN)
CREATE TABLE procurement.goods_receipts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    warehouse_id uuid NOT NULL REFERENCES inventory.warehouses(id) ON DELETE RESTRICT,
    purchase_order_id uuid NOT NULL REFERENCES procurement.purchase_orders(id) ON DELETE RESTRICT,
    receipt_number varchar(50) NOT NULL UNIQUE,
    delivery_note_number varchar(50) NOT NULL, -- Nomor Surat Jalan Vendor
    receipt_date date NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'Received', 'Cancelled')),
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL
);

CREATE TABLE procurement.goods_receipt_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    goods_receipt_id uuid NOT NULL REFERENCES procurement.goods_receipts(id) ON DELETE CASCADE,
    po_item_id uuid NOT NULL REFERENCES procurement.purchase_order_items(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES master.products(id) ON DELETE RESTRICT,
    quantity_received numeric(14, 4) NOT NULL CHECK (quantity_received > 0)
);

-- 6.4 Faktur Tagihan Pemasok (Vendor Bills)
CREATE TABLE procurement.vendor_bills (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    supplier_id uuid NOT NULL REFERENCES master.suppliers(id) ON DELETE RESTRICT,
    purchase_order_id uuid NOT NULL REFERENCES procurement.purchase_orders(id) ON DELETE RESTRICT,
    bill_number varchar(50) NOT NULL UNIQUE,
    vendor_invoice_number varchar(50) NOT NULL,
    bill_date date NOT NULL,
    due_date date NOT NULL,
    total_amount numeric(18, 2) NOT NULL CHECK (total_amount >= 0),
    paid_amount numeric(18, 2) NOT NULL DEFAULT 0 CHECK (paid_amount >= 0),
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'Posted', 'PartiallyPaid', 'Paid', 'Cancelled')),
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL
);
```

---

## 7. Definisi Schema: `sales` (Penjualan & Distribusi)

```sql
-- 7.1 Penawaran Harga (Sales Quotation - SQ)
CREATE TABLE sales.quotations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    customer_id uuid NOT NULL REFERENCES master.customers(id) ON DELETE RESTRICT,
    quotation_number varchar(50) NOT NULL UNIQUE,
    quotation_date date NOT NULL,
    expiration_date date NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'Sent', 'Approved', 'Rejected', 'Converted', 'Expired')),
    subtotal_amount numeric(18, 2) NOT NULL DEFAULT 0,
    tax_amount numeric(18, 2) NOT NULL DEFAULT 0,
    total_amount numeric(18, 2) NOT NULL DEFAULT 0,
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    is_deleted boolean NOT NULL DEFAULT FALSE
);

CREATE TABLE sales.quotation_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    quotation_id uuid NOT NULL REFERENCES sales.quotations(id) ON DELETE CASCADE,
    product_id uuid NOT NULL REFERENCES master.products(id) ON DELETE RESTRICT,
    quantity numeric(14, 4) NOT NULL CHECK (quantity > 0),
    unit_price numeric(18, 4) NOT NULL CHECK (unit_price >= 0),
    discount_rate numeric(5, 2) NOT NULL DEFAULT 0,
    line_total numeric(18, 2) NOT NULL CHECK (line_total >= 0)
);

-- 7.2 Pesanan Penjualan (Sales Order - SO)
CREATE TABLE sales.sales_orders (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    customer_id uuid NOT NULL REFERENCES master.customers(id) ON DELETE RESTRICT,
    quotation_id uuid REFERENCES sales.quotations(id) ON DELETE SET NULL,
    order_number varchar(50) NOT NULL UNIQUE,
    order_date date NOT NULL,
    expected_delivery_date date,
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'Confirmed', 'PartiallyShipped', 'FullyShipped', 'Invoiced', 'Closed', 'Cancelled')),
    subtotal_amount numeric(18, 2) NOT NULL DEFAULT 0 CHECK (subtotal_amount >= 0),
    tax_amount numeric(18, 2) NOT NULL DEFAULT 0 CHECK (tax_amount >= 0),
    total_amount numeric(18, 2) NOT NULL DEFAULT 0 CHECK (total_amount >= 0),
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    approved_by_user_id uuid REFERENCES identity.users(id),
    is_deleted boolean NOT NULL DEFAULT FALSE
);

CREATE TABLE sales.sales_order_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    sales_order_id uuid NOT NULL REFERENCES sales.sales_orders(id) ON DELETE CASCADE,
    product_id uuid NOT NULL REFERENCES master.products(id) ON DELETE RESTRICT,
    quantity numeric(14, 4) NOT NULL CHECK (quantity > 0),
    shipped_quantity numeric(14, 4) NOT NULL DEFAULT 0 CHECK (shipped_quantity >= 0),
    unit_price numeric(18, 4) NOT NULL CHECK (unit_price >= 0),
    discount_rate numeric(5, 2) NOT NULL DEFAULT 0,
    line_total numeric(18, 2) NOT NULL CHECK (line_total >= 0)
);

-- 7.3 Surat Jalan / Pengiriman (Delivery Order - DO)
CREATE TABLE sales.deliveries (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    warehouse_id uuid NOT NULL REFERENCES inventory.warehouses(id) ON DELETE RESTRICT,
    sales_order_id uuid NOT NULL REFERENCES sales.sales_orders(id) ON DELETE RESTRICT,
    delivery_number varchar(50) NOT NULL UNIQUE,
    delivery_date date NOT NULL,
    tracking_number varchar(50),
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'Dispatched', 'Delivered', 'Cancelled')),
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL
);

CREATE TABLE sales.delivery_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    delivery_id uuid NOT NULL REFERENCES sales.deliveries(id) ON DELETE CASCADE,
    so_item_id uuid NOT NULL REFERENCES sales.sales_order_items(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES master.products(id) ON DELETE RESTRICT,
    quantity_shipped numeric(14, 4) NOT NULL CHECK (quantity_shipped > 0)
);

-- 7.4 Faktur Penjualan (Sales Invoices)
CREATE TABLE sales.sales_invoices (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    customer_id uuid NOT NULL REFERENCES master.customers(id) ON DELETE RESTRICT,
    sales_order_id uuid NOT NULL REFERENCES sales.sales_orders(id) ON DELETE RESTRICT,
    invoice_number varchar(50) NOT NULL UNIQUE,
    invoice_date date NOT NULL,
    due_date date NOT NULL,
    tax_invoice_number varchar(50),
    subtotal_amount numeric(18, 2) NOT NULL CHECK (subtotal_amount >= 0),
    tax_amount numeric(18, 2) NOT NULL CHECK (tax_amount >= 0),
    total_amount numeric(18, 2) NOT NULL CHECK (total_amount >= 0),
    paid_amount numeric(18, 2) NOT NULL DEFAULT 0 CHECK (paid_amount >= 0),
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'Posted', 'PartiallyPaid', 'Paid', 'Cancelled')),
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL
);
```

---

## 8. Definisi Schema: `finance` (Buku Besar & Kas/Bank)

```sql
-- 8.1 Bagan Akun (Chart of Accounts - COA)
CREATE TABLE finance.chart_of_accounts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    account_code varchar(30) NOT NULL,
    account_name varchar(100) NOT NULL,
    account_type varchar(20) NOT NULL CHECK (account_type IN ('Asset', 'Liability', 'Equity', 'Revenue', 'Expense')),
    normal_balance varchar(2) NOT NULL CHECK (normal_balance IN ('DR', 'CR')),
    parent_account_id uuid REFERENCES finance.chart_of_accounts(id) ON DELETE RESTRICT,
    is_header boolean NOT NULL DEFAULT FALSE,
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_coa_company_code UNIQUE (company_id, account_code)
);

-- 8.2 Periode Fiskal
CREATE TABLE finance.fiscal_periods (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    fiscal_year integer NOT NULL,
    period_number integer NOT NULL CHECK (period_number BETWEEN 1 AND 12),
    start_date date NOT NULL,
    end_date date NOT NULL,
    is_closed boolean NOT NULL DEFAULT FALSE,
    closed_at_utc timestamptz,
    closed_by_user_id uuid REFERENCES identity.users(id),
    CONSTRAINT uq_fiscal_period UNIQUE (company_id, fiscal_year, period_number)
);

-- 8.3 Jurnal Umum (Journal Entries & Lines) - ENFORCE DOUBLE ENTRY BALANCING
CREATE TABLE finance.journal_entries (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    journal_number varchar(50) NOT NULL UNIQUE,
    journal_date date NOT NULL,
    reference_type varchar(30) NOT NULL, -- e.g., 'SalesInvoice', 'GoodsReceipt', 'Manual'
    reference_number varchar(50) NOT NULL,
    description text NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'Posted', 'Reversed')),
    total_debit numeric(18, 2) NOT NULL DEFAULT 0 CHECK (total_debit >= 0),
    total_credit numeric(18, 2) NOT NULL DEFAULT 0 CHECK (total_credit >= 0),
    posted_at_utc timestamptz,
    posted_by_user_id uuid REFERENCES identity.users(id),
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL,
    CONSTRAINT chk_journal_balance CHECK (total_debit = total_credit)
);

CREATE TABLE finance.journal_lines (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    journal_entry_id uuid NOT NULL REFERENCES finance.journal_entries(id) ON DELETE CASCADE,
    account_id uuid NOT NULL REFERENCES finance.chart_of_accounts(id) ON DELETE RESTRICT,
    debit numeric(18, 2) NOT NULL DEFAULT 0 CHECK (debit >= 0),
    credit numeric(18, 2) NOT NULL DEFAULT 0 CHECK (credit >= 0),
    description text,
    CONSTRAINT chk_line_either_debit_or_credit CHECK (
        (debit > 0 AND credit = 0) OR (credit > 0 AND debit = 0)
    )
);

-- 8.4 Akun Kas & Bank
CREATE TABLE finance.cash_bank_accounts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    account_id uuid NOT NULL REFERENCES finance.chart_of_accounts(id) ON DELETE RESTRICT,
    code varchar(20) NOT NULL,
    name varchar(100) NOT NULL,
    bank_name varchar(50),
    account_number varchar(50),
    currency varchar(3) NOT NULL DEFAULT 'IDR',
    is_active boolean NOT NULL DEFAULT TRUE,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_cash_bank_code UNIQUE (company_id, code)
);

-- 8.5 Transaksi Pembayaran (Payments & Allocations)
CREATE TABLE finance.payments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE RESTRICT,
    branch_id uuid NOT NULL REFERENCES master.branches(id) ON DELETE RESTRICT,
    cash_bank_account_id uuid NOT NULL REFERENCES finance.cash_bank_accounts(id) ON DELETE RESTRICT,
    payment_number varchar(50) NOT NULL UNIQUE,
    payment_date date NOT NULL,
    payment_type varchar(20) NOT NULL CHECK (payment_type IN ('CustomerReceipt', 'VendorPayment')),
    partner_id uuid NOT NULL, -- FK ke master.customers(id) atau master.suppliers(id)
    payment_method varchar(30) NOT NULL CHECK (payment_method IN ('BankTransfer', 'Cash', 'Giro', 'Cheque')),
    total_amount numeric(18, 2) NOT NULL CHECK (total_amount > 0),
    status varchar(20) NOT NULL CHECK (status IN ('Draft', 'Posted', 'Voided')),
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id uuid NOT NULL
);

CREATE TABLE finance.payment_allocations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_id uuid NOT NULL REFERENCES finance.payments(id) ON DELETE CASCADE,
    invoice_id uuid NOT NULL, -- FK ke sales.sales_invoices atau procurement.vendor_bills
    allocated_amount numeric(18, 2) NOT NULL CHECK (allocated_amount > 0)
);
```

---

## 9. Definisi Schema: `audit` (Jejak Rekam & Tata Kelola Sistem)

```sql
-- 9.1 Tabel Jejak Audit Imutabel (Partitioned by Year)
CREATE TABLE audit.audit_logs (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL,
    entity_name varchar(100) NOT NULL,
    entity_id uuid NOT NULL,
    action varchar(20) NOT NULL CHECK (action IN ('INSERT', 'UPDATE', 'DELETE', 'SOFT_DELETE')),
    old_values jsonb,
    new_values jsonb,
    user_id uuid NOT NULL,
    ip_address varchar(45),
    created_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id, created_at_utc)
) PARTITION BY RANGE (created_at_utc);

-- Partisi Aktif
CREATE TABLE audit.audit_logs_y2026 PARTITION OF audit.audit_logs
    FOR VALUES FROM ('2026-01-01 00:00:00+00') TO ('2027-01-01 00:00:00+00');

CREATE TABLE audit.audit_logs_y2027 PARTITION OF audit.audit_logs
    FOR VALUES FROM ('2027-01-01 00:00:00+00') TO ('2028-01-01 00:00:00+00');

-- 9.2 Pengaturan Konfigurasi Sistem
CREATE TABLE audit.system_settings (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE CASCADE,
    setting_key varchar(100) NOT NULL,
    setting_value text NOT NULL,
    description text,
    updated_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by_user_id uuid NOT NULL,
    CONSTRAINT uq_settings_company_key UNIQUE (company_id, setting_key)
);

-- 9.3 Metadata Lampiran Dokumen Digital
CREATE TABLE audit.document_attachments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES master.companies(id) ON DELETE CASCADE,
    entity_name varchar(100) NOT NULL, -- e.g., 'PurchaseOrder', 'VendorBill'
    entity_id uuid NOT NULL,
    file_name varchar(255) NOT NULL,
    file_extension varchar(10) NOT NULL,
    file_size_bytes bigint NOT NULL CHECK (file_size_bytes > 0 AND file_size_bytes <= 5242880), -- Maks 5MB
    storage_path text NOT NULL,
    uploaded_at_utc timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    uploaded_by_user_id uuid NOT NULL
);
```

