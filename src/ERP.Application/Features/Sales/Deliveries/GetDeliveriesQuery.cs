using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Sales.Deliveries;

[RequirePermission(Permissions.Sales.DeliveryDispatch)]
public record GetDeliveriesQuery(
    Guid? WarehouseId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<DeliveryDto>>>;

public class GetDeliveriesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetDeliveriesQuery, Result<PagedResult<DeliveryDto>>>
{
    public async Task<Result<PagedResult<DeliveryDto>>> Handle(
        GetDeliveriesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Deliveries
            .Include(d => d.Warehouse)
            .Include(d => d.SalesOrder)
                .ThenInclude(s => s.Customer)
            .Include(d => d.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Uom)
            .AsNoTracking();

        if (request.WarehouseId.HasValue && request.WarehouseId != Guid.Empty)
        {
            query = query.Where(d => d.WarehouseId == request.WarehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(d => d.DeliveryNumber.ToLower().Contains(search)
                || d.SalesOrder.OrderNumber.ToLower().Contains(search)
                || d.SalesOrder.Customer.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.DeliveryDate)
            .ThenByDescending(d => d.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DeliveryDto(
                d.Id,
                d.DeliveryNumber,
                d.WarehouseId,
                d.Warehouse.Name,
                d.SalesOrderId,
                d.SalesOrder.OrderNumber,
                d.SalesOrder.Customer.Name,
                d.DeliveryDate,
                d.TrackingNumber,
                d.Status.ToString(),
                d.CreatedAtUtc,
                d.Items.Select(i => new DeliveryItemDto(
                    i.Id,
                    i.SoItemId,
                    i.ProductId,
                    i.Product.Sku,
                    i.Product.Name,
                    i.Product.Uom.Code,
                    i.QuantityShipped
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<DeliveryDto>>.Success(
            new PagedResult<DeliveryDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

