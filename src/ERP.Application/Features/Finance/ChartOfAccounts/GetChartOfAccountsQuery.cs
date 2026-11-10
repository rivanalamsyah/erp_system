using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Finance.ChartOfAccounts;

[RequirePermission(Permissions.Finance.CoaView)]
public record GetChartOfAccountsQuery(
    string? AccountType = null,
    string? SearchTerm = null) : IRequest<Result<IReadOnlyList<ChartOfAccountDto>>>;

public class GetChartOfAccountsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetChartOfAccountsQuery, Result<IReadOnlyList<ChartOfAccountDto>>>
{
    public async Task<Result<IReadOnlyList<ChartOfAccountDto>>> Handle(
        GetChartOfAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.ChartOfAccounts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.AccountType))
        {
            var typeSearch = request.AccountType.Trim();
            query = query.Where(c => c.AccountType.ToString() == typeSearch);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(c => c.AccountCode.ToLower().Contains(search)
                || c.AccountName.ToLower().Contains(search));
        }

        var accounts = await query
            .OrderBy(c => c.AccountCode)
            .Select(c => new ChartOfAccountDto(
                c.Id,
                c.AccountCode,
                c.AccountName,
                c.AccountType.ToString(),
                c.NormalBalance.ToString(),
                c.ParentAccountId,
                c.IsHeader,
                c.IsActive,
                c.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ChartOfAccountDto>>.Success(accounts);
    }
}

