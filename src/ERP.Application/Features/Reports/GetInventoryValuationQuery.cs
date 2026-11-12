using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Reports;

[RequirePermission(Permissions.Reports.FinancialView)]
public record GetInventoryValuationQuery(Guid? WarehouseId = null) : IRequest<Result<InventoryValuationDto>>;

public class GetInventoryValuationQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetInventoryValuationQuery, Result<InventoryValuationDto>>
{
    public async Task<Result<InventoryValuationDto>> Handle(GetInventoryValuationQuery request, CancellationToken cancellationToken)
    {
        var balancesQuery = context.StockBalances
            .AsNoTracking();

        if (request.WarehouseId.HasValue && request.WarehouseId.Value != Guid.Empty)
        {
            balancesQuery = balancesQuery.Where(b => b.WarehouseId == request.WarehouseId.Value);
        }

        var items = await balancesQuery
            .OrderBy(b => b.Warehouse.Name)
            .ThenBy(b => b.Product.Name)
            .Select(b => new StockValuationItemDto(
                b.WarehouseId,
                b.Warehouse.Name,
                b.ProductId,
                b.Product.Sku,
                b.Product.Name,
                b.Product.Uom.Code,
                b.PhysicalQuantity,
                b.ReservedQuantity,
                b.PhysicalQuantity - b.ReservedQuantity,
                b.AverageCost > 0 ? b.AverageCost : b.Product.PurchasePrice,
                b.PhysicalQuantity * (b.AverageCost > 0 ? b.AverageCost : b.Product.PurchasePrice)
            ))
            .ToListAsync(cancellationToken);

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
