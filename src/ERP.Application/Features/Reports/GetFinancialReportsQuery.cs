using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Finance;
using ERP.Shared;
using ERP.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Reports;

[RequirePermission(Permissions.Reports.FinancialView)]
public record GetBalanceSheetQuery(Guid CompanyId, DateOnly AsOfDate) : IRequest<Result<BalanceSheetDto>>;

[RequirePermission(Permissions.Reports.FinancialView)]
public record GetProfitAndLossQuery(Guid CompanyId, DateOnly StartDate, DateOnly EndDate) : IRequest<Result<ProfitAndLossDto>>;

[RequirePermission(Permissions.Reports.FinancialView)]
public record GetTrialBalanceQuery(Guid CompanyId, DateOnly AsOfDate) : IRequest<Result<TrialBalanceDto>>;

public class FinancialReportsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService) :
    IRequestHandler<GetBalanceSheetQuery, Result<BalanceSheetDto>>,
    IRequestHandler<GetProfitAndLossQuery, Result<ProfitAndLossDto>>,
    IRequestHandler<GetTrialBalanceQuery, Result<TrialBalanceDto>>
{
    public async Task<Result<BalanceSheetDto>> Handle(GetBalanceSheetQuery request, CancellationToken cancellationToken)
    {
        var companyId = request.CompanyId != Guid.Empty ? request.CompanyId : currentUserService.CompanyId;

        var accounts = await context.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.IsActive && !a.IsHeader)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var accountTotals = await context.JournalLines
            .AsNoTracking()
            .Where(l => l.JournalEntry.CompanyId == companyId &&
                        l.JournalEntry.Status == JournalStatus.Posted &&
                        l.JournalEntry.JournalDate <= request.AsOfDate)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                TotalDebit = g.Sum(x => x.Debit),
                TotalCredit = g.Sum(x => x.Credit)
            })
            .ToDictionaryAsync(x => x.AccountId, cancellationToken);

        var balances = new List<AccountBalanceDto>(accounts.Count);
        decimal totalRevenue = 0m;
        decimal totalExpense = 0m;

        foreach (var acc in accounts)
        {
            var totalDr = accountTotals.TryGetValue(acc.Id, out var agg) ? agg.TotalDebit : 0m;
            var totalCr = agg != null ? agg.TotalCredit : 0m;
            var net = acc.NormalBalance == NormalBalance.DR ? (totalDr - totalCr) : (totalCr - totalDr);

            if (acc.AccountType == AccountType.Revenue)
            {
                totalRevenue += (totalCr - totalDr);
            }
            else if (acc.AccountType == AccountType.Expense)
            {
                totalExpense += (totalDr - totalCr);
            }

            balances.Add(new AccountBalanceDto(
                acc.Id,
                acc.AccountCode,
                acc.AccountName,
                acc.AccountType,
                totalDr,
                totalCr,
                net
            ));
        }

        var assets = balances.Where(b => b.AccountType == AccountType.Asset).ToList();
        var liabilities = balances.Where(b => b.AccountType == AccountType.Liability).ToList();
        var equity = balances.Where(b => b.AccountType == AccountType.Equity).ToList();

        var currentEarnings = totalRevenue - totalExpense;
        var totalAssets = assets.Sum(a => a.NetBalance);
        var totalLiabilities = liabilities.Sum(l => l.NetBalance);
        var totalEquity = equity.Sum(e => e.NetBalance) + currentEarnings;
        var totalLiabAndEquity = totalLiabilities + totalEquity;

        var sheet = new BalanceSheetDto(
            assets,
            liabilities,
            equity,
            totalAssets,
            totalLiabilities,
            totalEquity,
            totalLiabAndEquity,
            Math.Abs(totalAssets - totalLiabAndEquity) < 0.01m
        );

        return Result<BalanceSheetDto>.Success(sheet);
    }

    public async Task<Result<ProfitAndLossDto>> Handle(GetProfitAndLossQuery request, CancellationToken cancellationToken)
    {
        var companyId = request.CompanyId != Guid.Empty ? request.CompanyId : currentUserService.CompanyId;

        var accounts = await context.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.IsActive && !a.IsHeader &&
                        (a.AccountType == AccountType.Revenue || a.AccountType == AccountType.Expense))
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var accountTotals = await context.JournalLines
            .AsNoTracking()
            .Where(l => l.JournalEntry.CompanyId == companyId &&
                        l.JournalEntry.Status == JournalStatus.Posted &&
                        l.JournalEntry.JournalDate >= request.StartDate &&
                        l.JournalEntry.JournalDate <= request.EndDate)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                TotalDebit = g.Sum(x => x.Debit),
                TotalCredit = g.Sum(x => x.Credit)
            })
            .ToDictionaryAsync(x => x.AccountId, cancellationToken);

        var balances = new List<AccountBalanceDto>(accounts.Count);
        foreach (var acc in accounts)
        {
            var totalDr = accountTotals.TryGetValue(acc.Id, out var agg) ? agg.TotalDebit : 0m;
            var totalCr = agg != null ? agg.TotalCredit : 0m;
            var net = acc.NormalBalance == NormalBalance.CR ? (totalCr - totalDr) : (totalDr - totalCr);

            balances.Add(new AccountBalanceDto(
                acc.Id,
                acc.AccountCode,
                acc.AccountName,
                acc.AccountType,
                totalDr,
                totalCr,
                net
            ));
        }

        var revenues = balances.Where(b => b.AccountType == AccountType.Revenue).ToList();
        var cogs = balances.Where(b => b.AccountType == AccountType.Expense &&
            (b.AccountCode.StartsWith('5') ||
             b.AccountName.Contains("hpp", StringComparison.OrdinalIgnoreCase) ||
             b.AccountName.Contains("pokok", StringComparison.OrdinalIgnoreCase))).ToList();
        var cogsIds = cogs.Select(c => c.AccountId).ToHashSet();
        var expenses = balances.Where(b => b.AccountType == AccountType.Expense && !cogsIds.Contains(b.AccountId)).ToList();

        var totalRevenue = revenues.Sum(r => r.NetBalance);
        var totalCogs = cogs.Sum(c => c.NetBalance);
        var grossProfit = totalRevenue - totalCogs;
        var totalExpenses = expenses.Sum(e => e.NetBalance);
        var netProfit = grossProfit - totalExpenses;

        var pnl = new ProfitAndLossDto(
            revenues,
            cogs,
            expenses,
            totalRevenue,
            totalCogs,
            grossProfit,
            totalExpenses,
            netProfit
        );

        return Result<ProfitAndLossDto>.Success(pnl);
    }

    public async Task<Result<TrialBalanceDto>> Handle(GetTrialBalanceQuery request, CancellationToken cancellationToken)
    {
        var companyId = request.CompanyId != Guid.Empty ? request.CompanyId : currentUserService.CompanyId;

        var accounts = await context.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.IsActive && !a.IsHeader)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var accountTotals = await context.JournalLines
            .AsNoTracking()
            .Where(l => l.JournalEntry.CompanyId == companyId &&
                        l.JournalEntry.Status == JournalStatus.Posted &&
                        l.JournalEntry.JournalDate <= request.AsOfDate)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                TotalDebit = g.Sum(x => x.Debit),
                TotalCredit = g.Sum(x => x.Credit)
            })
            .ToDictionaryAsync(x => x.AccountId, cancellationToken);

        var list = new List<AccountBalanceDto>(accounts.Count);
        decimal grandTotalDebit = 0m;
        decimal grandTotalCredit = 0m;

        foreach (var acc in accounts)
        {
            var totalDr = accountTotals.TryGetValue(acc.Id, out var agg) ? agg.TotalDebit : 0m;
            var totalCr = agg != null ? agg.TotalCredit : 0m;
            var net = acc.NormalBalance == NormalBalance.DR ? (totalDr - totalCr) : (totalCr - totalDr);

            grandTotalDebit += totalDr;
            grandTotalCredit += totalCr;

            list.Add(new AccountBalanceDto(
                acc.Id,
                acc.AccountCode,
                acc.AccountName,
                acc.AccountType,
                totalDr,
                totalCr,
                net
            ));
        }

        var tb = new TrialBalanceDto(
            list,
            grandTotalDebit,
            grandTotalCredit,
            Math.Abs(grandTotalDebit - grandTotalCredit) < 0.01m
        );

        return Result<TrialBalanceDto>.Success(tb);
    }
}
