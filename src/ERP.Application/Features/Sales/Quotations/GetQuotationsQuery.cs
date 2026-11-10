using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Sales.Quotations;

[RequirePermission(Permissions.Sales.QuotationView)]
public record GetQuotationsQuery(
    Guid? CustomerId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<QuotationDto>>>;

public class GetQuotationsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetQuotationsQuery, Result<PagedResult<QuotationDto>>>
{
    public async Task<Result<PagedResult<QuotationDto>>> Handle(
        GetQuotationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Uom)
            .AsNoTracking();

        if (request.CustomerId.HasValue && request.CustomerId != Guid.Empty)
        {
            query = query.Where(q => q.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(q => q.QuotationNumber.ToLower().Contains(search)
                || q.Customer.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(q => q.QuotationDate)
            .ThenByDescending(q => q.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(q => new QuotationDto(
                q.Id,
                q.QuotationNumber,
                q.CustomerId,
                q.Customer.Name,
                q.QuotationDate,
                q.ExpirationDate,
                q.Status.ToString(),
                q.SubtotalAmount,
                q.TaxAmount,
                q.TotalAmount,
                q.CreatedAtUtc,
                q.Items.Select(i => new QuotationItemDto(
                    i.Id,
                    i.ProductId,
                    i.Product.Sku,
                    i.Product.Name,
                    i.Product.Uom.Code,
                    i.Quantity,
                    i.UnitPrice,
                    i.DiscountRate,
                    i.LineTotal
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<QuotationDto>>.Success(
            new PagedResult<QuotationDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

