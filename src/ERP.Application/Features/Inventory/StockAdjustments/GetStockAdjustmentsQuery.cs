using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Inventory.StockAdjustments;

[RequirePermission(Permissions.Inventory.StockView)]
public record GetStockAdjustmentsQuery(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<StockAdjustmentDto>>>;

public class GetStockAdjustmentsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetStockAdjustmentsQuery, Result<PagedResult<StockAdjustmentDto>>>
{
    public async Task<Result<PagedResult<StockAdjustmentDto>>> Handle(
        GetStockAdjustmentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.StockAdjustments
            .Include(a => a.Warehouse)
            .Include(a => a.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Uom)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.AdjustmentNumber.ToLower().Contains(search) ||
                a.Warehouse.Name.ToLower().Contains(search) ||
                a.Reason.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.AdjustmentDate)
            .ThenByDescending(a => a.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new StockAdjustmentDto(
                a.Id,
                a.AdjustmentNumber,
                a.WarehouseId,
                a.Warehouse.Name,
                a.AdjustmentDate,
                a.Status.ToString(),
                a.Reason,
                a.CreatedAtUtc,
                a.Items.Select(i => new StockAdjustmentItemDto(
                    i.Id,
                    i.ProductId,
                    i.Product.Sku,
                    i.Product.Name,
                    i.Product.Uom.Code,
                    i.SystemQuantity,
                    i.PhysicalQuantity,
                    i.DifferenceQuantity,
                    i.UnitCost)).ToList()))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<StockAdjustmentDto>>.Success(
            new PagedResult<StockAdjustmentDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

