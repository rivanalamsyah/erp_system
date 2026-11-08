using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Domain.Common;
using ERP.Domain.Entities.Audit;
using ERP.Domain.Entities.Master;
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

        // 3. Pemetaan Schema Audit
        builder.Entity<AuditLog>(b =>
        {
            b.ToTable("audit_logs", "audit");
            b.HasKey(x => x.Id);
            b.Property(x => x.OldValues).HasColumnType("jsonb");
            b.Property(x => x.NewValues).HasColumnType("jsonb");
        });
    }
}

