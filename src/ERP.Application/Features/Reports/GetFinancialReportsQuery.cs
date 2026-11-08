using ERP.Application.Common.Interfaces;
using ERP.Domain.Entities.Finance;
using ERP.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Reports;

public record GetBalanceSheetQuery(Guid CompanyId, DateOnly AsOfDate) : IRequest<Result<BalanceSheetDto>>;
public record GetProfitAndLossQuery(Guid CompanyId, DateOnly StartDate, DateOnly EndDate) : IRequest<Result<ProfitAndLossDto>>;
public record GetTrialBalanceQuery(Guid CompanyId, DateOnly AsOfDate) : IRequest<Result<TrialBalanceDto>>;

public class FinancialReportsQueryHandler :
    IRequestHandler<GetBalanceSheetQuery, Result<BalanceSheetDto>>,
    IRequestHandler<GetProfitAndLossQuery, Result<ProfitAndLossDto>>,
    IRequestHandler<GetTrialBalanceQuery, Result<TrialBalanceDto>>
{
    private readonly IApplicationDbContext _context;

    public FinancialReportsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BalanceSheetDto>> Handle(GetBalanceSheetQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _context.ChartOfAccounts
            .Where(a => a.CompanyId == request.CompanyId && a.IsActive)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var postedLines = await _context.JournalLines
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.CompanyId == request.CompanyId &&
                        l.JournalEntry.Status == JournalStatus.Posted &&
                        l.JournalEntry.JournalDate <= request.AsOfDate)
            .ToListAsync(cancellationToken);

        var balances = new List<AccountBalanceDto>();
        foreach (var acc in accounts)
        {
            var lines = postedLines.Where(l => l.AccountId == acc.Id).ToList();
            var totalDr = lines.Sum(l => l.Debit);
            var totalCr = lines.Sum(l => l.Credit);

            var net = acc.NormalBalance == NormalBalance.DR ? (totalDr - totalCr) : (totalCr - totalDr);

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

        // Calculate current earnings (Revenues - Expenses)
        var revLines = postedLines.Where(l => accounts.Any(a => a.Id == l.AccountId && a.AccountType == AccountType.Revenue)).ToList();
        var expLines = postedLines.Where(l => accounts.Any(a => a.Id == l.AccountId && a.AccountType == AccountType.Expense)).ToList();
        var currentEarnings = revLines.Sum(l => l.Credit - l.Debit) - expLines.Sum(l => l.Debit - l.Credit);

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
        var accounts = await _context.ChartOfAccounts
            .Where(a => a.CompanyId == request.CompanyId && a.IsActive)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var postedLines = await _context.JournalLines
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.CompanyId == request.CompanyId &&
                        l.JournalEntry.Status == JournalStatus.Posted &&
                        l.JournalEntry.JournalDate >= request.StartDate &&
                        l.JournalEntry.JournalDate <= request.EndDate)
            .ToListAsync(cancellationToken);

        var balances = new List<AccountBalanceDto>();
        foreach (var acc in accounts)
        {
            var lines = postedLines.Where(l => l.AccountId == acc.Id).ToList();
            var totalDr = lines.Sum(l => l.Debit);
            var totalCr = lines.Sum(l => l.Credit);
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
        var cogs = balances.Where(b => b.AccountType == AccountType.Expense && (b.AccountCode.StartsWith("5") || b.AccountName.ToLower().Contains("hpp") || b.AccountName.ToLower().Contains("pokok"))).ToList();
        var expenses = balances.Where(b => b.AccountType == AccountType.Expense && !cogs.Any(c => c.AccountId == b.AccountId)).ToList();

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
        var accounts = await _context.ChartOfAccounts
            .Where(a => a.CompanyId == request.CompanyId && a.IsActive)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var postedLines = await _context.JournalLines
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.CompanyId == request.CompanyId &&
                        l.JournalEntry.Status == JournalStatus.Posted &&
                        l.JournalEntry.JournalDate <= request.AsOfDate)
            .ToListAsync(cancellationToken);

        var list = new List<AccountBalanceDto>();
        decimal grandTotalDebit = 0;
        decimal grandTotalCredit = 0;

        foreach (var acc in accounts)
        {
            var lines = postedLines.Where(l => l.AccountId == acc.Id).ToList();
            var totalDr = lines.Sum(l => l.Debit);
            var totalCr = lines.Sum(l => l.Credit);
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
