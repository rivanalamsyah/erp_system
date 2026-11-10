using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Sales.SalesOrders;

[RequirePermission(Permissions.Sales.OrderCreate)]
public record GetSalesOrdersQuery(
    Guid? CustomerId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<SalesOrderDto>>>;

public class GetSalesOrdersQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetSalesOrdersQuery, Result<PagedResult<SalesOrderDto>>>
{
    public async Task<Result<PagedResult<SalesOrderDto>>> Handle(
        GetSalesOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Quotation)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Uom)
            .AsNoTracking();

        if (request.CustomerId.HasValue && request.CustomerId != Guid.Empty)
        {
            query = query.Where(s => s.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(s => s.OrderNumber.ToLower().Contains(search)
                || s.Customer.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.OrderDate)
            .ThenByDescending(s => s.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SalesOrderDto(
                s.Id,
                s.OrderNumber,
                s.CustomerId,
                s.Customer.Name,
                s.QuotationId,
                s.Quotation != null ? s.Quotation.QuotationNumber : null,
                s.OrderDate,
                s.ExpectedDeliveryDate,
                s.Status.ToString(),
                s.SubtotalAmount,
                s.TaxAmount,
                s.TotalAmount,
                s.CreatedAtUtc,
                s.Items.Select(i => new SalesOrderItemDto(
                    i.Id,
                    i.ProductId,
                    i.Product.Sku,
                    i.Product.Name,
                    i.Product.Uom.Code,
                    i.Quantity,
                    i.ShippedQuantity,
                    i.UnitPrice,
                    i.DiscountRate,
                    i.LineTotal
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<SalesOrderDto>>.Success(
            new PagedResult<SalesOrderDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

