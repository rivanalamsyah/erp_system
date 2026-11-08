using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Procurement.GoodsReceipts;

[RequirePermission(Permissions.Procurement.GrnView)]
public record GetGoodsReceiptsQuery(
    Guid? WarehouseId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<GoodsReceiptDto>>;

public class GetGoodsReceiptsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetGoodsReceiptsQuery, PagedResult<GoodsReceiptDto>>
{
    public async Task<PagedResult<GoodsReceiptDto>> Handle(
        GetGoodsReceiptsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.GoodsReceipts
            .Include(g => g.Warehouse)
            .Include(g => g.PurchaseOrder)
                .ThenInclude(p => p.Supplier)
            .Include(g => g.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Uom)
            .AsNoTracking();

        if (request.WarehouseId.HasValue && request.WarehouseId != Guid.Empty)
        {
            query = query.Where(g => g.WarehouseId == request.WarehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(g => g.ReceiptNumber.ToLower().Contains(search)
                || g.DeliveryNoteNumber.ToLower().Contains(search)
                || g.PurchaseOrder.PoNumber.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(g => g.ReceiptDate)
            .ThenByDescending(g => g.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(g => new GoodsReceiptDto(
                g.Id,
                g.ReceiptNumber,
                g.DeliveryNoteNumber,
                g.WarehouseId,
                g.Warehouse.Name,
                g.PurchaseOrderId,
                g.PurchaseOrder.PoNumber,
                g.PurchaseOrder.Supplier.Name,
                g.ReceiptDate,
                g.Status.ToString(),
                g.CreatedAtUtc,
                g.Items.Select(i => new GoodsReceiptItemDto(
                    i.Id,
                    i.PoItemId,
                    i.ProductId,
                    i.Product.Sku,
                    i.Product.Name,
                    i.Product.Uom.Code,
                    i.QuantityReceived
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<GoodsReceiptDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
