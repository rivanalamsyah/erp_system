using Microsoft.EntityFrameworkCore;
using ERP.Domain.Entities.Audit;
using ERP.Domain.Entities.Master;

namespace ERP.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Master Data
    DbSet<Company> Companies { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Department> Departments { get; }
    DbSet<UnitOfMeasure> UnitsOfMeasure { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<Product> Products { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<DocumentSequence> DocumentSequences { get; }

    // Audit
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

