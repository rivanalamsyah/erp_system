namespace ERP.Application.Features.Finance.ChartOfAccounts;

public record ChartOfAccountDto(
    Guid Id,
    string AccountCode,
    string AccountName,
    string AccountType,
    string NormalBalance,
    Guid? ParentAccountId,
    bool IsHeader,
    bool IsActive,
    DateTime CreatedAtUtc);

