using ERP.Domain.Entities.Finance;

namespace ERP.Application.Features.Reports;

public record AccountBalanceDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    AccountType AccountType,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal NetBalance
);

public record BalanceSheetDto(
    List<AccountBalanceDto> Assets,
    List<AccountBalanceDto> Liabilities,
    List<AccountBalanceDto> Equity,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal TotalLiabilitiesAndEquity,
    bool IsBalanced
);

public record ProfitAndLossDto(
    List<AccountBalanceDto> Revenues,
    List<AccountBalanceDto> CostOfGoodsSold,
    List<AccountBalanceDto> Expenses,
    decimal TotalRevenue,
    decimal TotalCogs,
    decimal GrossProfit,
    decimal TotalExpenses,
    decimal NetProfit
);

public record TrialBalanceDto(
    List<AccountBalanceDto> Accounts,
    decimal TotalDebit,
    decimal TotalCredit,
    bool IsBalanced
);
