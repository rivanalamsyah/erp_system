using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Domain.Entities.Master;
using ERP.Shared.Common;

namespace ERP.Application.Features.Master.Products;

public record GetProductsQuery(
    Guid? CompanyId = null,
    Guid? CategoryId = null,
    ProductType? ProductType = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<ProductDto>>>;

public class GetProductsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    public async Task<Result<PagedResult<ProductDto>>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Products
            .Include(p => p.Category)
            .Include(p => p.Uom)
            .AsNoTracking();

        if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CompanyId == request.CompanyId.Value);
        }

        if (request.CategoryId.HasValue && request.CategoryId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (request.ProductType.HasValue)
        {
            query = query.Where(p => p.ProductType == request.ProductType.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(p =>
                p.Sku.ToLower().Contains(search) ||
                p.Name.ToLower().Contains(search) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Sku)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto(
                p.Id,
                p.CompanyId,
                p.CategoryId,
                p.Category.Name,
                p.UomId,
                p.Uom.Code,
                p.Sku,
                p.Barcode,
                p.Name,
                p.Description,
                p.ProductType,
                p.PurchasePrice,
                p.SellingPrice,
                p.MinimumStockLevel,
                p.IsActive,
                p.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<ProductDto>>.Success(
            new PagedResult<ProductDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

