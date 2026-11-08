using ERP.Domain.Common;

namespace ERP.Domain.Entities.Master;

public class Department : BaseEntity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public Company Company { get; private set; } = default!;

    private Department() { }

    public static Department Create(Guid companyId, string code, string name, Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Department
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

