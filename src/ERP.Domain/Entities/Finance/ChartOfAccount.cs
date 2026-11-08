using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Finance;

public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense
}

public enum NormalBalance
{
    DR,
    CR
}

public class ChartOfAccount : BaseEntity<Guid>
{
    public string AccountCode { get; private set; } = string.Empty;
    public string AccountName { get; private set; } = string.Empty;
    public AccountType AccountType { get; private set; }
    public NormalBalance NormalBalance { get; private set; }
    public Guid? ParentAccountId { get; private set; }
    public bool IsHeader { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Company Company { get; private set; } = default!;
    public ChartOfAccount? ParentAccount { get; private set; }
    public ICollection<ChartOfAccount> SubAccounts { get; private set; } = [];

    private ChartOfAccount() { }

    public static ChartOfAccount Create(
        Guid companyId,
        string accountCode,
        string accountName,
        AccountType accountType,
        NormalBalance normalBalance,
        Guid createdByUserId,
        Guid? parentAccountId = null,
        bool isHeader = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);

        return new ChartOfAccount
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            AccountCode = accountCode.Trim().ToUpperInvariant(),
            AccountName = accountName.Trim(),
            AccountType = accountType,
            NormalBalance = normalBalance,
            ParentAccountId = parentAccountId,
            IsHeader = isHeader,
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(string accountName, AccountType accountType, NormalBalance normalBalance, bool isHeader, bool isActive, Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);

        AccountName = accountName.Trim();
        AccountType = accountType;
        NormalBalance = normalBalance;
        IsHeader = isHeader;
        IsActive = isActive;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
