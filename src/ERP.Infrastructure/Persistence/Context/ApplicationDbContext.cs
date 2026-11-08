using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Domain.Common;
using ERP.Domain.Entities.Audit;
using ERP.Domain.Entities.Inventory;
using ERP.Domain.Entities.Master;
using ERP.Domain.Entities.Procurement;
using ERP.Domain.Entities.Sales;
using ERP.Domain.Entities.Finance;
using ERP.Infrastructure.Identity.Models;
using ERP.Infrastructure.Persistence.Interceptors;

namespace ERP.Infrastructure.Persistence.Context;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUserService currentUserService,
    AuditSaveChangesInterceptor auditInterceptor)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IApplicationDbContext
{
    // Master Data
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();

    // Inventory
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentItem> StockAdjustmentItems => Set<StockAdjustmentItem>();

    // Procurement
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchaseRequestItem> PurchaseRequestItems => Set<PurchaseRequestItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
    public DbSet<VendorBill> VendorBills => Set<VendorBill>();

    // Sales
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryItem> DeliveryItems => Set<DeliveryItem>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();

    // Finance
    public DbSet<ChartOfAccount> ChartOfAccounts => Set<ChartOfAccount>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<CashBankAccount> CashBankAccounts => Set<CashBankAccount>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

    // Identity Custom Tables
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Pemetaan Schema Identity
        builder.Entity<ApplicationUser>().ToTable("users", "identity");
        builder.Entity<ApplicationRole>().ToTable("roles", "identity");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("user_roles", "identity");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("user_claims", "identity");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("user_logins", "identity");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("role_claims", "identity");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("user_tokens", "identity");

        builder.Entity<Permission>(b =>
        {
            b.ToTable("permissions", "identity");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<RolePermission>(b =>
        {
            b.ToTable("role_permissions", "identity");
            b.HasKey(x => new { x.RoleId, x.PermissionId });
            b.HasOne(x => x.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(x => x.RoleId);
            b.HasOne(x => x.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(x => x.PermissionId);
        });

        builder.Entity<UserBranch>(b =>
        {
            b.ToTable("user_branches", "identity");
            b.HasKey(x => new { x.UserId, x.BranchId });
            b.HasOne(x => x.User)
                .WithMany(u => u.UserBranches)
                .HasForeignKey(x => x.UserId);
        });

        builder.Entity<UserSession>(b =>
        {
            b.ToTable("user_sessions", "identity");
            b.HasKey(x => x.Id);
            b.HasOne(x => x.User)
                .WithMany(u => u.UserSessions)
                .HasForeignKey(x => x.UserId);
        });

        // 2. Pemetaan Schema Master
        builder.Entity<Company>(b =>
        {
            b.ToTable("companies", "master");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<Branch>(b =>
        {
            b.ToTable("branches", "master");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            b.HasOne(x => x.Company)
                .WithMany(c => c.Branches)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<Department>(b =>
        {
            b.ToTable("departments", "master");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            b.HasOne(x => x.Company)
                .WithMany(c => c.Departments)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<UnitOfMeasure>(b =>
        {
            b.ToTable("units_of_measure", "master");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            b.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<ProductCategory>(b =>
        {
            b.ToTable("product_categories", "master");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            b.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ParentCategory)
                .WithMany(p => p.SubCategories)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<Product>(b =>
        {
            b.ToTable("products", "master");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CompanyId, x.Sku }).IsUnique();
            b.Property(x => x.PurchasePrice).HasPrecision(18, 4);
            b.Property(x => x.SellingPrice).HasPrecision(18, 4);
            b.Property(x => x.MinimumStockLevel).HasPrecision(14, 4);
            b.Property(x => x.ProductType).HasConversion<string>();
            b.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Uom)
                .WithMany()
                .HasForeignKey(x => x.UomId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<Customer>(b =>
        {
            b.ToTable("customers", "master");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            b.Property(x => x.CreditLimit).HasPrecision(18, 2);
            b.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<Supplier>(b =>
        {
            b.ToTable("suppliers", "master");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            b.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<DocumentSequence>(b =>
        {
            b.ToTable("document_sequences", "master");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CompanyId, x.BranchId, x.DocumentType, x.Year, x.Month }).IsUnique();
            b.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. Pemetaan Schema Inventory
        builder.Entity<Warehouse>(b =>
        {
            b.ToTable("warehouses", "inventory");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<StockBalance>(b =>
        {
            b.ToTable("stock_balances", "inventory");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.WarehouseId, x.ProductId }).IsUnique();
            b.Property(x => x.PhysicalQuantity).HasPrecision(14, 4);
            b.Property(x => x.ReservedQuantity).HasPrecision(14, 4);
            b.Property(x => x.AverageCost).HasPrecision(18, 4);
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId);
        });

        builder.Entity<StockMovement>(b =>
        {
            b.ToTable("stock_movements", "inventory");
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).HasPrecision(14, 4);
            b.Property(x => x.UnitCost).HasPrecision(18, 4);
            b.Property(x => x.TotalCost).HasPrecision(18, 2);
            b.Property(x => x.MovementType).HasConversion<string>();
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId);
        });

        builder.Entity<StockTransfer>(b =>
        {
            b.ToTable("stock_transfers", "inventory");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.TransferNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.SourceWarehouse).WithMany().HasForeignKey(x => x.SourceWarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.DestinationWarehouse).WithMany().HasForeignKey(x => x.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<StockTransferItem>(b =>
        {
            b.ToTable("stock_transfer_items", "inventory");
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).HasPrecision(14, 4);
            b.HasOne(x => x.Transfer).WithMany(t => t.Items).HasForeignKey(x => x.TransferId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StockAdjustment>(b =>
        {
            b.ToTable("stock_adjustments", "inventory");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.AdjustmentNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<StockAdjustmentItem>(b =>
        {
            b.ToTable("stock_adjustment_items", "inventory");
            b.HasKey(x => x.Id);
            b.Property(x => x.SystemQuantity).HasPrecision(14, 4);
            b.Property(x => x.PhysicalQuantity).HasPrecision(14, 4);
            b.Property(x => x.DifferenceQuantity).HasPrecision(14, 4);
            b.Property(x => x.UnitCost).HasPrecision(18, 4);
            b.HasOne(x => x.Adjustment).WithMany(a => a.Items).HasForeignKey(x => x.AdjustmentId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // 4. Pemetaan Schema Procurement
        builder.Entity<PurchaseRequest>(b =>
        {
            b.ToTable("purchase_requests", "procurement");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.RequestNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<PurchaseRequestItem>(b =>
        {
            b.ToTable("purchase_request_items", "procurement");
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).HasPrecision(14, 4);
            b.Property(x => x.EstimatedUnitPrice).HasPrecision(18, 4);
            b.HasOne(x => x.Request).WithMany(r => r.Items).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PurchaseOrder>(b =>
        {
            b.ToTable("purchase_orders", "procurement");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.PoNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
            b.Property(x => x.TaxAmount).HasPrecision(18, 2);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<PurchaseOrderItem>(b =>
        {
            b.ToTable("purchase_order_items", "procurement");
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).HasPrecision(14, 4);
            b.Property(x => x.ReceivedQuantity).HasPrecision(14, 4);
            b.Property(x => x.UnitPrice).HasPrecision(18, 4);
            b.Property(x => x.DiscountRate).HasPrecision(5, 2);
            b.Property(x => x.LineTotal).HasPrecision(18, 2);
            b.HasOne(x => x.PurchaseOrder).WithMany(p => p.Items).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GoodsReceipt>(b =>
        {
            b.ToTable("goods_receipts", "procurement");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.ReceiptNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<GoodsReceiptItem>(b =>
        {
            b.ToTable("goods_receipt_items", "procurement");
            b.HasKey(x => x.Id);
            b.Property(x => x.QuantityReceived).HasPrecision(14, 4);
            b.HasOne(x => x.GoodsReceipt).WithMany(g => g.Items).HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.PurchaseOrderItem).WithMany().HasForeignKey(x => x.PoItemId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<VendorBill>(b =>
        {
            b.ToTable("vendor_bills", "procurement");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.BillNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.PaidAmount).HasPrecision(18, 2);
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        // 5. Pemetaan Schema Sales
        builder.Entity<Quotation>(b =>
        {
            b.ToTable("quotations", "sales");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.QuotationNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
            b.Property(x => x.TaxAmount).HasPrecision(18, 2);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<QuotationItem>(b =>
        {
            b.ToTable("quotation_items", "sales");
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).HasPrecision(14, 4);
            b.Property(x => x.UnitPrice).HasPrecision(18, 4);
            b.Property(x => x.DiscountRate).HasPrecision(5, 2);
            b.Property(x => x.LineTotal).HasPrecision(18, 2);
            b.HasOne(x => x.Quotation).WithMany(q => q.Items).HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SalesOrder>(b =>
        {
            b.ToTable("sales_orders", "sales");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.OrderNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
            b.Property(x => x.TaxAmount).HasPrecision(18, 2);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Quotation).WithMany().HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.SetNull);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<SalesOrderItem>(b =>
        {
            b.ToTable("sales_order_items", "sales");
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).HasPrecision(14, 4);
            b.Property(x => x.ShippedQuantity).HasPrecision(14, 4);
            b.Property(x => x.UnitPrice).HasPrecision(18, 4);
            b.Property(x => x.DiscountRate).HasPrecision(5, 2);
            b.Property(x => x.LineTotal).HasPrecision(18, 2);
            b.HasOne(x => x.SalesOrder).WithMany(s => s.Items).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Delivery>(b =>
        {
            b.ToTable("deliveries", "sales");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.DeliveryNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<DeliveryItem>(b =>
        {
            b.ToTable("delivery_items", "sales");
            b.HasKey(x => x.Id);
            b.Property(x => x.QuantityShipped).HasPrecision(14, 4);
            b.HasOne(x => x.Delivery).WithMany(d => d.Items).HasForeignKey(x => x.DeliveryId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.SalesOrderItem).WithMany().HasForeignKey(x => x.SoItemId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SalesInvoice>(b =>
        {
            b.ToTable("sales_invoices", "sales");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.InvoiceNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
            b.Property(x => x.TaxAmount).HasPrecision(18, 2);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.PaidAmount).HasPrecision(18, 2);
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        // 6. Pemetaan Schema Finance
        builder.Entity<ChartOfAccount>(b =>
        {
            b.ToTable("chart_of_accounts", "finance");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CompanyId, x.AccountCode }).IsUnique();
            b.Property(x => x.AccountType).HasConversion<string>();
            b.Property(x => x.NormalBalance).HasConversion<string>();
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ParentAccount).WithMany(p => p.SubAccounts).HasForeignKey(x => x.ParentAccountId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<FiscalPeriod>(b =>
        {
            b.ToTable("fiscal_periods", "finance");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CompanyId, x.FiscalYear, x.PeriodNumber }).IsUnique();
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JournalEntry>(b =>
        {
            b.ToTable("journal_entries", "finance");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.JournalNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.TotalDebit).HasPrecision(18, 2);
            b.Property(x => x.TotalCredit).HasPrecision(18, 2);
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<JournalLine>(b =>
        {
            b.ToTable("journal_lines", "finance");
            b.HasKey(x => x.Id);
            b.Property(x => x.Debit).HasPrecision(18, 2);
            b.Property(x => x.Credit).HasPrecision(18, 2);
            b.HasOne(x => x.JournalEntry).WithMany(j => j.Lines).HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CashBankAccount>(b =>
        {
            b.ToTable("cash_bank_accounts", "finance");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<Payment>(b =>
        {
            b.ToTable("payments", "finance");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.PaymentNumber).IsUnique();
            b.Property(x => x.PaymentType).HasConversion<string>();
            b.Property(x => x.PaymentMethod).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.CashBankAccount).WithMany().HasForeignKey(x => x.CashBankAccountId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).IsRowVersion();
            b.HasQueryFilter(x => !x.IsDeleted && (currentUserService.CompanyId == Guid.Empty || x.CompanyId == currentUserService.CompanyId));
        });

        builder.Entity<PaymentAllocation>(b =>
        {
            b.ToTable("payment_allocations", "finance");
            b.HasKey(x => x.Id);
            b.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
            b.HasOne(x => x.Payment).WithMany(p => p.Allocations).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
        });

        // 7. Pemetaan Schema Audit
        builder.Entity<AuditLog>(b =>
        {
            b.ToTable("audit_logs", "audit");
            b.HasKey(x => x.Id);
            b.Property(x => x.OldValues).HasColumnType("jsonb");
            b.Property(x => x.NewValues).HasColumnType("jsonb");
        });
    }
}

