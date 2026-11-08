using ERP.Domain.Common;

namespace ERP.Domain.Entities.Master;

public class UnitOfMeasure : BaseEntity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public Company Company { get; private set; } = default!;

    private UnitOfMeasure() { }

    public static UnitOfMeasure Create(Guid companyId, string code, string name, Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(string name, Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class ProductCategory : BaseEntity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentCategoryId { get; private set; }

    public Company Company { get; private set; } = default!;
    public ProductCategory? ParentCategory { get; private set; }
    public ICollection<ProductCategory> SubCategories { get; private set; } = [];

    private ProductCategory() { }

    public static ProductCategory Create(Guid companyId, string code, string name, Guid? parentCategoryId, Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ProductCategory
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            ParentCategoryId = parentCategoryId,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(string name, Guid? parentCategoryId, Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        ParentCategoryId = parentCategoryId;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

