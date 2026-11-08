using ERP.Domain.Common;

namespace ERP.Domain.Entities.Master;

public class Customer : BaseEntity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string BillingAddress { get; private set; } = string.Empty;
    public string? ShippingAddress { get; private set; }
    public string? TaxNumber { get; private set; }
    public decimal CreditLimit { get; private set; }
    public int PaymentTermsDays { get; private set; } = 30;
    public bool IsActive { get; private set; } = true;

    public Company Company { get; private set; } = default!;

    private Customer() { }

    public static Customer Create(
        Guid companyId,
        string code,
        string name,
        string? email,
        string? phone,
        string billingAddress,
        string? shippingAddress,
        string? taxNumber,
        decimal creditLimit,
        int paymentTermsDays,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(billingAddress);
        if (creditLimit < 0) throw new ArgumentOutOfRangeException(nameof(creditLimit), "Limit kredit tidak boleh negatif.");
        if (paymentTermsDays < 0) throw new ArgumentOutOfRangeException(nameof(paymentTermsDays), "Termin bayar tidak boleh negatif.");

        return new Customer
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Email = email?.Trim(),
            Phone = phone?.Trim(),
            BillingAddress = billingAddress.Trim(),
            ShippingAddress = shippingAddress?.Trim(),
            TaxNumber = taxNumber?.Trim(),
            CreditLimit = creditLimit,
            PaymentTermsDays = paymentTermsDays,
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(
        string name,
        string? email,
        string? phone,
        string billingAddress,
        string? shippingAddress,
        string? taxNumber,
        decimal creditLimit,
        int paymentTermsDays,
        bool isActive,
        Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(billingAddress);
        if (creditLimit < 0) throw new ArgumentOutOfRangeException(nameof(creditLimit), "Limit kredit tidak boleh negatif.");
        if (paymentTermsDays < 0) throw new ArgumentOutOfRangeException(nameof(paymentTermsDays), "Termin bayar tidak boleh negatif.");

        Name = name.Trim();
        Email = email?.Trim();
        Phone = phone?.Trim();
        BillingAddress = billingAddress.Trim();
        ShippingAddress = shippingAddress?.Trim();
        TaxNumber = taxNumber?.Trim();
        CreditLimit = creditLimit;
        PaymentTermsDays = paymentTermsDays;
        IsActive = isActive;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class Supplier : BaseEntity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public string? TaxNumber { get; private set; }
    public int PaymentTermsDays { get; private set; } = 30;
    public string? BankAccountName { get; private set; }
    public string? BankAccountNumber { get; private set; }
    public string? BankName { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Company Company { get; private set; } = default!;

    private Supplier() { }

    public static Supplier Create(
        Guid companyId,
        string code,
        string name,
        string? email,
        string? phone,
        string address,
        string? taxNumber,
        int paymentTermsDays,
        string? bankAccountName,
        string? bankAccountNumber,
        string? bankName,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        if (paymentTermsDays < 0) throw new ArgumentOutOfRangeException(nameof(paymentTermsDays), "Termin bayar tidak boleh negatif.");

        return new Supplier
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Email = email?.Trim(),
            Phone = phone?.Trim(),
            Address = address.Trim(),
            TaxNumber = taxNumber?.Trim(),
            PaymentTermsDays = paymentTermsDays,
            BankAccountName = bankAccountName?.Trim(),
            BankAccountNumber = bankAccountNumber?.Trim(),
            BankName = bankName?.Trim(),
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(
        string name,
        string? email,
        string? phone,
        string address,
        string? taxNumber,
        int paymentTermsDays,
        string? bankAccountName,
        string? bankAccountNumber,
        string? bankName,
        bool isActive,
        Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        if (paymentTermsDays < 0) throw new ArgumentOutOfRangeException(nameof(paymentTermsDays), "Termin bayar tidak boleh negatif.");

        Name = name.Trim();
        Email = email?.Trim();
        Phone = phone?.Trim();
        Address = address.Trim();
        TaxNumber = taxNumber?.Trim();
        PaymentTermsDays = paymentTermsDays;
        BankAccountName = bankAccountName?.Trim();
        BankAccountNumber = bankAccountNumber?.Trim();
        BankName = bankName?.Trim();
        IsActive = isActive;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

