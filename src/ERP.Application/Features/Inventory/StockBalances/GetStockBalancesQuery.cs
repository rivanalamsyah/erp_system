using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Shared;

namespace ERP.Application.Features.Inventory.StockBalances;

public record GetStockBalancesQuery(
    Guid? WarehouseId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<StockBalanceDto>>>;

public class GetStockBalancesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetStockBalancesQuery, Result<PagedResult<StockBalanceDto>>>
{
    public async Task<Result<PagedResult<StockBalanceDto>>> Handle(
        GetStockBalancesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.StockBalances
            .Include(sb => sb.Warehouse)
            .Include(sb => sb.Product)
            .ThenInclude(p => p.Uom)
            .AsNoTracking();

        if (request.WarehouseId.HasValue && request.WarehouseId.Value != Guid.Empty)
        {
            query = query.Where(sb => sb.WarehouseId == request.WarehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(sb =>
                sb.Product.Sku.ToLower().Contains(search) ||
                sb.Product.Name.ToLower().Contains(search) ||
                sb.Warehouse.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(sb => sb.Product.Sku)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(sb => new StockBalanceDto(
                sb.Id,
                sb.WarehouseId,
                sb.Warehouse.Code,
                sb.Warehouse.Name,
                sb.ProductId,
                sb.Product.Sku,
                sb.Product.Name,
                sb.Product.Uom.Code,
                sb.PhysicalQuantity,
                sb.ReservedQuantity,
                sb.PhysicalQuantity - sb.ReservedQuantity,
                sb.AverageCost,
                Math.Round(sb.PhysicalQuantity * sb.AverageCost, 2),
                sb.LastUpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<StockBalanceDto>>.Success(
            new PagedResult<StockBalanceDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

public record GetStockCardQuery(
    Guid WarehouseId,
    Guid ProductId,
    int Limit = 50) : IRequest<Result<IReadOnlyList<StockMovementDto>>>;

public class GetStockCardQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetStockCardQuery, Result<IReadOnlyList<StockMovementDto>>>
{
    public async Task<Result<IReadOnlyList<StockMovementDto>>> Handle(
        GetStockCardQuery request,
        CancellationToken cancellationToken)
    {
        var movements = await context.StockMovements
            .Where(m => m.WarehouseId == request.WarehouseId && m.ProductId == request.ProductId)
            .OrderByDescending(m => m.MovementDateUtc)
            .Take(request.Limit)
            .Select(m => new StockMovementDto(
                m.Id,
                m.MovementType.ToString(),
                m.Quantity,
                m.Direction,
                m.UnitCost,
                m.TotalCost,
                m.ReferenceNumber,
                m.MovementDateUtc))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<StockMovementDto>>.Success(movements);
    }
}

