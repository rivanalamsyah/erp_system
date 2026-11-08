using ERP.Domain.Common;

namespace ERP.Domain.Entities.Master;

public class Branch : BaseEntity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public bool IsHeadOffice { get; private set; }

    public Company Company { get; private set; } = default!;

    private Branch() { }

    public static Branch Create(
        Guid companyId,
        string code,
        string name,
        string? address,
        string? phone,
        bool isHeadOffice,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Branch
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Address = address?.Trim(),
            Phone = phone?.Trim(),
            IsHeadOffice = isHeadOffice,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(
        string name,
        string? address,
        string? phone,
        bool isHeadOffice,
        Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Address = address?.Trim();
        Phone = phone?.Trim();
        IsHeadOffice = isHeadOffice;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

