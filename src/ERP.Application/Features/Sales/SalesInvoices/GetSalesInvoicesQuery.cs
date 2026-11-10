using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Sales.SalesInvoices;

[RequirePermission(Permissions.Sales.InvoiceIssue)]
public record GetSalesInvoicesQuery(
    Guid? CustomerId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<SalesInvoiceDto>>>;

public class GetSalesInvoicesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetSalesInvoicesQuery, Result<PagedResult<SalesInvoiceDto>>>
{
    public async Task<Result<PagedResult<SalesInvoiceDto>>> Handle(
        GetSalesInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.SalesOrder)
            .AsNoTracking();

        if (request.CustomerId.HasValue && request.CustomerId != Guid.Empty)
        {
            query = query.Where(i => i.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(i => i.InvoiceNumber.ToLower().Contains(search)
                || (i.TaxInvoiceNumber != null && i.TaxInvoiceNumber.ToLower().Contains(search))
                || i.SalesOrder.OrderNumber.ToLower().Contains(search)
                || i.Customer.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new SalesInvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.TaxInvoiceNumber,
                i.CustomerId,
                i.Customer.Name,
                i.SalesOrderId,
                i.SalesOrder.OrderNumber,
                i.InvoiceDate,
                i.DueDate,
                i.SubtotalAmount,
                i.TaxAmount,
                i.TotalAmount,
                i.PaidAmount,
                i.RemainingAmount,
                i.Status.ToString(),
                i.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<SalesInvoiceDto>>.Success(
            new PagedResult<SalesInvoiceDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

