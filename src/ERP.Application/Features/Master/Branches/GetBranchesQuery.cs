using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Shared.Common;

namespace ERP.Application.Features.Master.Branches;

public record GetBranchesQuery(
    Guid? CompanyId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<BranchDto>>>;

public class GetBranchesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetBranchesQuery, Result<PagedResult<BranchDto>>>
{
    public async Task<Result<PagedResult<BranchDto>>> Handle(
        GetBranchesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Branches
            .Include(b => b.Company)
            .AsNoTracking();

        if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
        {
            query = query.Where(b => b.CompanyId == request.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(b =>
                b.Code.ToLower().Contains(search) ||
                b.Name.ToLower().Contains(search) ||
                (b.Address != null && b.Address.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.IsHeadOffice)
            .ThenBy(b => b.Code)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BranchDto(
                b.Id,
                b.CompanyId,
                b.Company.Name,
                b.Code,
                b.Name,
                b.Address,
                b.Phone,
                b.IsHeadOffice,
                b.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<BranchDto>>.Success(
            new PagedResult<BranchDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

