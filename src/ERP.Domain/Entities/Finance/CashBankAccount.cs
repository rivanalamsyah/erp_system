using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Finance;

public class CashBankAccount : BaseEntity<Guid>
{
    public Guid AccountId { get; private set; } // FK to COA
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? BankName { get; private set; }
    public string? AccountNumber { get; private set; }
    public string Currency { get; private set; } = "IDR";
    public bool IsActive { get; private set; } = true;

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public ChartOfAccount Account { get; private set; } = default!;

    private CashBankAccount() { }

    public static CashBankAccount Create(
        Guid companyId,
        Guid branchId,
        Guid accountId,
        string code,
        string name,
        string? bankName,
        string? accountNumber,
        Guid createdByUserId,
        string currency = "IDR")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new CashBankAccount
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            AccountId = accountId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            BankName = bankName?.Trim(),
            AccountNumber = accountNumber?.Trim(),
            Currency = currency.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(string name, string? bankName, string? accountNumber, bool isActive, Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        BankName = bankName?.Trim();
        AccountNumber = accountNumber?.Trim();
        IsActive = isActive;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
