using ERP.Domain.Common;

namespace ERP.Domain.Entities.Master;

public class Company : BaseEntity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string TaxIdentificationNumber { get; private set; } = string.Empty; // NPWP
    public string Address { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string CurrencyCode { get; private set; } = "IDR";

    public ICollection<Branch> Branches { get; private set; } = [];
    public ICollection<Department> Departments { get; private set; } = [];

    private Company() { }

    public static Company Create(
        string code,
        string name,
        string taxIdentificationNumber,
        string address,
        string? phone,
        string? email,
        Guid createdByUserId,
        string currencyCode = "IDR")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Company
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            TaxIdentificationNumber = taxIdentificationNumber.Trim(),
            Address = address.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            CurrencyCode = currencyCode.ToUpperInvariant(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(
        string name,
        string taxIdentificationNumber,
        string address,
        string? phone,
        string? email,
        Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        TaxIdentificationNumber = taxIdentificationNumber.Trim();
        Address = address.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

