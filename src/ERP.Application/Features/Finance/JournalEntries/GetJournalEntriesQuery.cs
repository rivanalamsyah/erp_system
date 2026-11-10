using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Finance.JournalEntries;

[RequirePermission(Permissions.Finance.JournalView)]
public record GetJournalEntriesQuery(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<JournalEntryDto>>>;

public class GetJournalEntriesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetJournalEntriesQuery, Result<PagedResult<JournalEntryDto>>>
{
    public async Task<Result<PagedResult<JournalEntryDto>>> Handle(
        GetJournalEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.JournalEntries
            .Include(j => j.Lines)
                .ThenInclude(l => l.Account)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(j => j.JournalNumber.ToLower().Contains(search)
                || j.ReferenceNumber.ToLower().Contains(search)
                || j.Description.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(j => j.JournalDate)
            .ThenByDescending(j => j.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(j => new JournalEntryDto(
                j.Id,
                j.JournalNumber,
                j.JournalDate,
                j.ReferenceType,
                j.ReferenceNumber,
                j.Description,
                j.Status.ToString(),
                j.TotalDebit,
                j.TotalCredit,
                j.PostedAtUtc,
                j.CreatedAtUtc,
                j.Lines.Select(l => new JournalLineDto(
                    l.Id,
                    l.AccountId,
                    l.Account.AccountCode,
                    l.Account.AccountName,
                    l.Debit,
                    l.Credit,
                    l.Description
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<JournalEntryDto>>.Success(
            new PagedResult<JournalEntryDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

