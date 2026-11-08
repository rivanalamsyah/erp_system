using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Shared;

namespace ERP.Application.Features.Procurement.PurchaseRequests;

public record GetPurchaseRequestsQuery(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<PurchaseRequestDto>>>;

public class GetPurchaseRequestsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetPurchaseRequestsQuery, Result<PagedResult<PurchaseRequestDto>>>
{
    public async Task<Result<PagedResult<PurchaseRequestDto>>> Handle(
        GetPurchaseRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.PurchaseRequests
            .Include(r => r.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Uom)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(r =>
                r.RequestNumber.ToLower().Contains(search) ||
                (r.Notes != null && r.Notes.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.RequestDate)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new PurchaseRequestDto(
                r.Id,
                r.RequestNumber,
                r.RequestDate,
                r.RequiredDate,
                r.Status.ToString(),
                r.Notes,
                r.CreatedAtUtc,
                r.Items.Select(i => new PurchaseRequestItemDto(
                    i.Id,
                    i.ProductId,
                    i.Product.Sku,
                    i.Product.Name,
                    i.Product.Uom.Code,
                    i.Quantity,
                    i.EstimatedUnitPrice)).ToList()))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<PurchaseRequestDto>>.Success(
            new PagedResult<PurchaseRequestDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}
