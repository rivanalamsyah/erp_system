# Architecture Changelog & Version History — Enterprise ERP System

All notable changes, architectural decisions, and baseline specifications for this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0-LTS Architecture Baseline] - 2026-11-08

### Added (Fase 0: Discovery, System Design & Database Architecture)
- **Master Documentation Portal & Index:**
  - Ditambahkan master portal di [`docs/README.md`](./README.md) dan root [`README.md`](../README.md).
- **Strategi Produk & Analisis Kebutuhan Bisnis:**
  - Disusun dokumen visi strategis enterprise di [`docs/PROJECT_OVERVIEW.md`](./PROJECT_OVERVIEW.md).
  - Disusun Product Requirement Document (PRD), 7 User Personas, Matriks RACI, Use Cases, dan prioritas MoSCoW di [`docs/PRODUCT_REQUIREMENTS.md`](./PRODUCT_REQUIREMENTS.md).
  - Didefinisikan kebutuhan bisnis tingkat korporat, model hierarki organisasi holding/subsidiaries, dan analisis ROI di [`docs/BUSINESS_REQUIREMENTS.md`](./BUSINESS_REQUIREMENTS.md).
  - Dirinci 60+ kebutuhan fungsional katalog (FR-SYS, FR-MDM, FR-IAM, FR-HRM, FR-INV, FR-PRC, FR-SLS, FR-FIN, FR-REP) di [`docs/FUNCTIONAL_REQUIREMENTS.md`](./FUNCTIONAL_REQUIREMENTS.md).
  - Ditetapkan standar metrik performa, skalabilitas (500 pengguna aktif), ketersediaan (99.9% uptime), dan observabilitas di [`docs/NON_FUNCTIONAL_REQUIREMENTS.md`](./NON_FUNCTIONAL_REQUIREMENTS.md).
- **Arsitektur Perangkat Lunak & Desain Sistem:**
  - Diformulasikan System Design Document (SDD) dengan diagram alur komponen di [`docs/SYSTEM_DESIGN.md`](./SYSTEM_DESIGN.md).
  - Dirancang panduan Clean Architecture + Modular Monolith (.NET 10 LTS, C# 14, CQRS MediatR, In-Process Domain Events) di [`docs/SOFTWARE_ARCHITECTURE.md`](./SOFTWARE_ARCHITECTURE.md).
  - Disusun 12 Architectural Decision Records (ADR-001 s.d. ADR-012) di [`docs/ARCHITECTURE_DECISION_RECORDS.md`](./ARCHITECTURE_DECISION_RECORDS.md).
  - Dirinci struktur fisik repositori dan solution layout di [`docs/PROJECT_STRUCTURE.md`](./PROJECT_STRUCTURE.md).
  - Ditetapkan standar coding modern C# 14 dan konvensi EF Core di [`docs/CODING_STANDARDS.md`](./CODING_STANDARDS.md).
- **Spesifikasi Modul & Alur Proses Bisnis:**
  - Dibuat spesifikasi fungsional mendalam untuk 9 modul ERP di [`docs/MODULE_SPECIFICATIONS.md`](./MODULE_SPECIFICATIONS.md).
  - Didesain alur kerja end-to-end (Order-to-Cash, Procure-to-Pay, Stock Transfer & Opname, Payroll, Month-End Close) lengkap dengan diagram alur Mermaid di [`docs/BUSINESS_WORKFLOWS.md`](./BUSINESS_WORKFLOWS.md).
  - Ditetapkan katalog aturan bisnis formal (BR-GEN, BR-SALES, BR-PROC, BR-INV, BR-ACC, BR-HR), validasi stok negatif, dan penegakan jurnal seimbang di [`docs/BUSINESS_RULES.md`](./BUSINESS_RULES.md).
- **Arsitektur Basis Data PostgreSQL & Tata Kelola Data:**
  - Dirancang filosofi arsitektur basis data, UUIDv7, presisi moneter `numeric(18,4)`, penomoran sekuensial anti-celah, dan kontrol konkurensi optimistik `xmin` di [`docs/DATABASE_DESIGN.md`](./DATABASE_DESIGN.md).
  - Didefinisikan DDL PostgreSQL lengkap untuk 8 domain schema (`identity`, `master`, `hr`, `inventory`, `procurement`, `sales`, `finance`, `audit`) di [`docs/DATABASE_SCHEMA.md`](./DATABASE_SCHEMA.md).
  - Dibuat Entity Relationship Diagram (ERD) visual Mermaid global dan per domain di [`docs/ENTITY_RELATIONSHIP_DIAGRAM.md`](./ENTITY_RELATIONSHIP_DIAGRAM.md).
  - Disusun Kamus Data (Data Dictionary) komprehensif mencakup tipe data, kunci, nullability, dan batasan constraint di [`docs/DATA_DICTIONARY.md`](./DATA_DICTIONARY.md).
- **Keamanan, Otorisasi & Aksesibilitas:**
  - Dimodelkan ancaman STRIDE, mitigasi OWASP Top 10, dan perlindungan anti-tamper log audit di [`docs/SECURITY_DESIGN.md`](./SECURITY_DESIGN.md).
  - Disusun matriks otorisasi Permission-Based Access Control (PBAC), data access scoping per cabang, dan pembatasan peran di [`docs/USER_ROLES_AND_PERMISSIONS.md`](./USER_ROLES_AND_PERMISSIONS.md).
  - Dirancang blueprint UI/UX MudBlazor, skema palet korporat, pola form master-detail, dan server-side datatables di [`docs/UI_UX_GUIDELINES.md`](./UI_UX_GUIDELINES.md).
- **Pengujian, Penerapan & Rencana Rekayasa:**
  - Dibuat strategi pengujian 5 tingkat piramida (xUnit, FluentAssertions, Testcontainers PostgreSQL, NetArchTest, BUnit) di [`docs/TESTING_STRATEGY.md`](./TESTING_STRATEGY.md).
  - Disusun panduan kontainer Docker multi-stage, orkestrasi Docker Compose, reverse proxy Nginx (WebSocket SignalR), dan prosedur backup di [`docs/DEPLOYMENT_GUIDE.md`](./DEPLOYMENT_GUIDE.md).
  - Dirancang roadmap strategis 16 minggu (8 sprint) di [`docs/DEVELOPMENT_ROADMAP.md`](./DEVELOPMENT_ROADMAP.md).
  - Dirinci Work Breakdown Structure (WBS) dan rencana mitigasi risiko di [`docs/IMPLEMENTATION_PLAN.md`](./IMPLEMENTATION_PLAN.md).
  - Disusun kamus istilah operasional, akuntansi, dan teknis di [`docs/GLOSSARY.md`](./GLOSSARY.md).

### Governance & Boundaries
- Sesuai instruksi tata kelola enterprise (Fase 0), seluruh fokus awal diarahkan pada analisis kebutuhan, perancangan arsitektur, dan dokumentasi teknis menyeluruh sebagai single source of truth.

---

## [1.1.0-Core Implementation] - 2026-11-08

### Added (Fase 1: Foundation, Authentication, PBAC, Master Data & Clean Architecture)
- **Environment & Solution Setup:**
  - Solusi modern `.NET 10 LTS` + `C# 14` (`EnterpriseERP.slnx`) dengan 8 project: `src/ERP.Shared`, `src/ERP.Domain`, `src/ERP.Application`, `src/ERP.Infrastructure`, `src/ERP.Web`, `tests/ERP.Domain.Tests`, `tests/ERP.Application.Tests`, `tests/ERP.Architecture.Tests`.
  - Database PostgreSQL 18 berjalan lokal di port `5433` dengan 8 database schema aktif.
- **Domain Modeling & Core Shared:**
  - `Result`, `Result<T>`, `Error`, `PagedResult<T>` di `ERP.Shared`.
  - Value Objects `Money` (immutable record struct dengan pencegahan currency mismatch) dan `DocumentNumber`.
  - `BaseEntity<TId>` dengan audit fields (`CreatedAtUtc`, `CreatedByUserId`, `UpdatedAtUtc`, `UpdatedByUserId`), soft delete (`SoftDelete`, `UndoSoftDelete`), dan optimistic concurrency `Version` (PostgreSQL `xmin`).
  - Entitas domain master: `Company`, `Branch`, `Department`, `UnitOfMeasure`, `ProductCategory`, `Product`, `Customer`, `Supplier`, `DocumentSequence`.
- **Identity & PBAC (Permission-Based Access Control):**
  - Implementasi entitas `ApplicationUser`, `ApplicationRole`, `Permission`, `RolePermission`, `UserBranch`, `UserSession`.
  - 11 Peran standar enterprise (`SUPER_ADMIN`, `DIRECTOR`, `FIN_MGR`, dll.) dan 34 permissions fungsional modular.
  - Matriks hak akses 127 relasi `RolePermission` sesuai spesifikasi otorisasi.
  - Auto-seeder database (`DatabaseSeeder.cs`) yang mengeksekusi inisialisasi baseline: default holding company, head office branch, departments, UoM, product categories, roles, permissions, dan Super Admin user (`admin@enterprise.erp`).
- **Entity Framework Core 10 & PostgreSQL Persistence:**
  - `ApplicationDbContext` dengan schema mapping terpisah untuk `identity`, `master`, dan `audit`.
  - Migrasi EF Core pertama: `20261108073655_InitialMasterAndIdentity` berhasil diaplikasikan ke PostgreSQL (22 tabel aktif).
  - `AuditSaveChangesInterceptor` otomatis mencatat riwayat perubahan data (audit log) ke tabel `audit.audit_logs`.
- **Application CQRS & Business Logic:**
  - CQRS Commands, Handlers, dan FluentValidation untuk `Companies` (Create, Update, Delete, GetCompanies paged query).
  - CQRS Commands, Handlers, dan FluentValidation untuk `Branches` (Create, Update, Delete, GetBranches paged query).
  - CQRS Commands, Handlers, dan FluentValidation untuk `Products` (Create, Update, Delete, GetProducts paged query).
  - Pipeline behaviors MediatR: `ValidationPipelineBehavior` dan `AuthorizationPipelineBehavior`.
- **Blazor Web App UI (MudBlazor Interactive Server):**
  - Tema korporat `ErpTheme.EnterpriseTheme` (Deep Navy `#1E3A8A`, Professional Teal `#0F766E`, Slate Background `#F8FAFC`).
  - `MainLayout` responsif lengkap dengan collapsible navigation drawer, active branch indicator (`HO-JKT`), top app bar, user profile dropdown, dan alert notification badge.
  - `NavMenu` modular mencakup 9 kelompok navigasi enterprise.
  - Halaman Otentikasi `/login` dengan validasi server-side dan layout `AuthLayout`.
  - Halaman Dashboard Eksekutif `/` dengan kartu KPI real-time terkoneksi database PostgreSQL.
  - Halaman Master Perusahaan `/master/companies` dengan server-side MudTable, pagination, searching, dialog form tambah/ubah, dan konfirmasi hapus.
  - Halaman Master Cabang `/master/branches` dengan filter dropdown perusahaan, pagination, dialog form, dan proteksi kantor pusat.
  - Halaman Master Produk `/master/products` dengan filter kategori, filter tipe produk, kalkulasi harga, format mata uang, dialog form, dan konfirmasi hapus.
- **Automated Testing Suite (100% Pass Rate):**
  - `ERP.Architecture.Tests`: 4 tes arsitektur NetArchTest memverifikasi kepatuhan Clean Architecture dan dependency rules.
  - `ERP.Domain.Tests`: 10 unit test memverifikasi operasi aritmatika `Money`, invariant domain, dan soft delete.
  - `ERP.Application.Tests`: 6 unit test memverifikasi FluentValidation pipeline untuk `CreateCompanyCommand` dan `CreateProductCommand`.
  - Total: **20 passed, 0 failed, 0 skipped**.

