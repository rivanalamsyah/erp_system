using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Procurement.PurchaseOrders;

[RequirePermission(Permissions.Procurement.PoView)]
public record GetPurchaseOrdersQuery(
    Guid? SupplierId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<PurchaseOrderDto>>>;

public class GetPurchaseOrdersQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetPurchaseOrdersQuery, Result<PagedResult<PurchaseOrderDto>>>
{
    public async Task<Result<PagedResult<PurchaseOrderDto>>> Handle(
        GetPurchaseOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Uom)
            .AsNoTracking();

        if (request.SupplierId.HasValue && request.SupplierId.Value != Guid.Empty)
        {
            query = query.Where(po => po.SupplierId == request.SupplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(po =>
                po.PoNumber.ToLower().Contains(search) ||
                po.Supplier.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(po => po.OrderDate)
            .ThenByDescending(po => po.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(po => new PurchaseOrderDto(
                po.Id,
                po.PoNumber,
                po.SupplierId,
                po.Supplier.Name,
                po.OrderDate,
                po.ExpectedDeliveryDate,
                po.Status.ToString(),
                po.SubtotalAmount,
                po.TaxAmount,
                po.TotalAmount,
                po.CreatedAtUtc,
                po.Items.Select(i => new PurchaseOrderItemDto(
                    i.Id,
                    i.ProductId,
                    i.Product.Sku,
                    i.Product.Name,
                    i.Product.Uom.Code,
                    i.Quantity,
                    i.ReceivedQuantity,
                    i.UnitPrice,
                    i.DiscountRate,
                    i.LineTotal)).ToList()))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<PurchaseOrderDto>>.Success(
            new PagedResult<PurchaseOrderDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

