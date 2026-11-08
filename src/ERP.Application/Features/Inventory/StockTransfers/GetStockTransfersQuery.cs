using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Shared;

namespace ERP.Application.Features.Inventory.StockTransfers;

public record GetStockTransfersQuery(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<StockTransferDto>>>;

public class GetStockTransfersQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetStockTransfersQuery, Result<PagedResult<StockTransferDto>>>
{
    public async Task<Result<PagedResult<StockTransferDto>>> Handle(
        GetStockTransfersQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.StockTransfers
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .Include(t => t.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Uom)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(t =>
                t.TransferNumber.ToLower().Contains(search) ||
                t.SourceWarehouse.Name.ToLower().Contains(search) ||
                t.DestinationWarehouse.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.TransferDate)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new StockTransferDto(
                t.Id,
                t.TransferNumber,
                t.SourceWarehouseId,
                t.SourceWarehouse.Name,
                t.DestinationWarehouseId,
                t.DestinationWarehouse.Name,
                t.TransferDate,
                t.Status.ToString(),
                t.Notes,
                t.CreatedAtUtc,
                t.Items.Select(i => new StockTransferItemDto(
                    i.Id,
                    i.ProductId,
                    i.Product.Sku,
                    i.Product.Name,
                    i.Product.Uom.Code,
                    i.Quantity)).ToList()))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<StockTransferDto>>.Success(
            new PagedResult<StockTransferDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

