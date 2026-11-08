# Enterprise Resource Planning (ERP) System

[![Architecture](https://img.shields.io/badge/Architecture-Modular%20Monolith%20%7C%20Clean-blue.svg)](docs/SOFTWARE_ARCHITECTURE.md)
[![Platform](https://img.shields.io/badge/.NET-10.0%20LTS-purple.svg)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/C%23-14.0-blue.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Frontend](https://img.shields.io/badge/Blazor-Interactive%20Server%20%2B%20MudBlazor-green.svg)](docs/UI_UX_GUIDELINES.md)
[![Database](https://img.shields.io/badge/PostgreSQL-16%2B%20%28EF%20Core%2010%29-blue.svg)](docs/DATABASE_DESIGN.md)

General-purpose, scalable, enterprise-grade ERP system engineered with **.NET 10 LTS, C# 14, Blazor Web App (Interactive Server), MudBlazor, PostgreSQL, and EF Core 10**.

---

## 📌 Status Fase Proyek: **Discovery, Architecture & Technical Design (Zero-Code Phase)**

> [!IMPORTANT]
> Proyek ini saat ini berada dalam **Fase 0 (Discovery, Analisis Bisnis & Desain Arsitektur Sistem)**.
> Sesuai tata kelola enterprise, **tidak ada source code aplikasi atau script migration yang dibuat** sebelum seluruh spesifikasi fungsional, perancangan basis data, aturan bisnis, dan arsitektur sistem disetujui secara resmi.

---

## 📚 Dokumentasi Lengkap Proyek

Seluruh dokumentasi teknis dan bisnis telah disusun secara terstruktur di dalam direktori [`/docs`](./docs/README.md).

### Akses Cepat Dokumen Inti:
1. **[Master Documentation Portal](./docs/README.md)** — Indeks dan sitemap seluruh dokumen arsitektur.
2. **[Project Overview](./docs/PROJECT_OVERVIEW.md)** — Visi bisnis, latar belakang, dan value proposition.
3. **[Product Requirements Document (PRD)](./docs/PRODUCT_REQUIREMENTS.md)** — Target pengguna, personas, use cases, prioritas MoSCoW.
4. **[System Design Document (SDD)](./docs/SYSTEM_DESIGN.md)** — Arsitektur teknis sistem, data flow, dan integrasi modul.
5. **[Software Architecture](./docs/SOFTWARE_ARCHITECTURE.md)** — Clean Architecture, Modular Monolith, aturan dependensi, CQRS.
6. **[Database Schema & DDL](./docs/DATABASE_SCHEMA.md)** — Spesifikasi DDL PostgreSQL 8 domain schema lengkap.
7. **[Entity Relationship Diagram (ERD)](./docs/ENTITY_RELATIONSHIP_DIAGRAM.md)** — Diagram ERD visual Mermaid.
8. **[Data Dictionary](./docs/DATA_DICTIONARY.md)** — Kamus tabel, kolom, data type, indeks, dan constraint.
9. **[Business Workflows](./docs/BUSINESS_WORKFLOWS.md)** — Alur proses bisnis lintas modul (Order-to-Cash, Procure-to-Pay, dll).
10. **[User Roles & Permissions](./docs/USER_ROLES_AND_PERMISSIONS.md)** — Permission-Based Access Control (PBAC) & isolasi cabang.
11. **[MudBlazor UI/UX Blueprint](./docs/UI_UX_GUIDELINES.md)** — Design system dan pola interaksi UI server-side.
12. **[Roadmap & Implementation Plan](./docs/IMPLEMENTATION_PLAN.md)** — Rencana implementasi teknis dan WBS fase konstruksi.

---

## 🏢 9 Modul Utama ERP
- **Dashboard & Analytics:** Executive KPI cards, sales trends, cash flow forecast, inventory alerts.
- **Master Data:** Multi-Company, Multi-Branch, Departemen, Jabatan, Karyawan, Pelanggan, Supplier, Produk & Kategori.
- **Identity & Access Management:** ASP.NET Core Identity, PBAC granuler, pembatasan data hierarkis (Company & Branch scope).
- **Human Resource Management:** Kehadiran (*Attendance*), Pengajuan Cuti (*Leave Management*), Penggajian (*Payroll Calculation*).
- **Inventory Management:** Multi-Gudang, Kartu Stok, Mutasi Masuk/Keluar, Transfer Antar Gudang, Stock Opname/Adjustment.
- **Procurement Management:** Purchase Request (PR), Approval Matrix, Purchase Order (PO), Goods Receipt Note (GRN), Vendor Bills.
- **Sales Management:** Sales Quotation, Sales Order (SO), Delivery Order (DO), Surat Jalan, Sales Invoice, Customer Invoicing.
- **Finance & Accounting:** Chart of Accounts (COA), Multi-Period Fiscal Year, Double-Entry General Ledger, Kas & Bank, Account Payables/Receivables.
- **Reporting & Governance:** Financial Statement (Neraca, Laba Rugi, Trial Balance), Tax Invoicing, Immutable Audit Trails.

---
*Enterprise ERP Architecture Group — 2026*

