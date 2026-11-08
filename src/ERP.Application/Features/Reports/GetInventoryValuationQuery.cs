using ERP.Application.Common.Interfaces;
using ERP.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Reports;

public record GetInventoryValuationQuery(Guid? WarehouseId = null) : IRequest<Result<InventoryValuationDto>>;

public class GetInventoryValuationQueryHandler : IRequestHandler<GetInventoryValuationQuery, Result<InventoryValuationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryValuationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<InventoryValuationDto>> Handle(GetInventoryValuationQuery request, CancellationToken cancellationToken)
    {
        var balancesQuery = _context.StockBalances
            .Include(b => b.Warehouse)
            .Include(b => b.Product)
                .ThenInclude(p => p.Uom)
            .AsNoTracking();

        if (request.WarehouseId.HasValue && request.WarehouseId.Value != Guid.Empty)
        {
            balancesQuery = balancesQuery.Where(b => b.WarehouseId == request.WarehouseId.Value);
        }

        var balances = await balancesQuery.ToListAsync(cancellationToken);

        var items = balances.Select(b =>
        {
            var unitCost = b.AverageCost > 0 ? b.AverageCost : (b.Product?.PurchasePrice ?? 0m);
            var valuation = b.PhysicalQuantity * unitCost;

            return new StockValuationItemDto(
                b.WarehouseId,
                b.Warehouse?.Name ?? "-",
                b.ProductId,
                b.Product?.Sku ?? "-",
                b.Product?.Name ?? "-",
                b.Product?.Uom?.Code ?? "PCS",
                b.PhysicalQuantity,
                b.ReservedQuantity,
                b.AvailableQuantity,
                unitCost,
                valuation
            );
        }).OrderBy(i => i.WarehouseName).ThenBy(i => i.ProductName).ToList();

        var grandTotalVal = items.Sum(i => i.TotalValuation);
        var grandTotalQty = items.Sum(i => i.PhysicalQuantity);

        var result = new InventoryValuationDto(
            grandTotalVal,
            grandTotalQty,
            items.Count,
            items
        );

        return Result<InventoryValuationDto>.Success(result);
    }
}
