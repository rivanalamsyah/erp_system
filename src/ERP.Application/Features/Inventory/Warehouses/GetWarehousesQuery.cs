using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Inventory.Warehouses;

[RequirePermission(Permissions.Inventory.StockView)]
public record GetWarehousesQuery(
    Guid? BranchId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<WarehouseDto>>>;

public class GetWarehousesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetWarehousesQuery, Result<PagedResult<WarehouseDto>>>
{
    public async Task<Result<PagedResult<WarehouseDto>>> Handle(
        GetWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Warehouses
            .Include(w => w.Branch)
            .AsNoTracking();

        if (request.BranchId.HasValue && request.BranchId.Value != Guid.Empty)
        {
            query = query.Where(w => w.BranchId == request.BranchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(w =>
                w.Code.ToLower().Contains(search) ||
                w.Name.ToLower().Contains(search) ||
                (w.Location != null && w.Location.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(w => w.Code)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(w => new WarehouseDto(
                w.Id,
                w.CompanyId,
                w.BranchId,
                w.Branch.Name,
                w.Code,
                w.Name,
                w.Location,
                w.IsActive,
                w.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<WarehouseDto>>.Success(
            new PagedResult<WarehouseDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

