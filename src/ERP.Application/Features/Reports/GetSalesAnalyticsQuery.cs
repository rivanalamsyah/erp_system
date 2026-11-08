using ERP.Application.Common.Interfaces;
using ERP.Domain.Entities.Sales;
using ERP.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Reports;

public record GetSalesAnalyticsQuery(Guid CompanyId, DateOnly? StartDate = null, DateOnly? EndDate = null) : IRequest<Result<SalesAnalyticsDto>>;

public class GetSalesAnalyticsQueryHandler : IRequestHandler<GetSalesAnalyticsQuery, Result<SalesAnalyticsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSalesAnalyticsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SalesAnalyticsDto>> Handle(GetSalesAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var invQuery = _context.SalesInvoices
            .Include(i => i.Customer)
            .Where(i => i.CompanyId == request.CompanyId && i.Status != SalesInvoiceStatus.Cancelled);

        if (request.StartDate.HasValue)
        {
            invQuery = invQuery.Where(i => i.InvoiceDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            invQuery = invQuery.Where(i => i.InvoiceDate <= request.EndDate.Value);
        }

        var invoices = await invQuery.ToListAsync(cancellationToken);

        var totalInvoiced = invoices.Sum(i => i.TotalAmount);
        var totalCollected = invoices.Sum(i => i.PaidAmount);
        var totalOutstandingAr = totalInvoiced - totalCollected;

        var totalOrders = await _context.SalesOrders
            .CountAsync(s => s.CompanyId == request.CompanyId, cancellationToken);

        // Top Customers
        var topCustomers = invoices
            .GroupBy(i => new { i.CustomerId, CustomerName = i.Customer != null ? i.Customer.Name : "Unknown" })
            .Select(g => new TopCustomerDto(
                g.Key.CustomerId,
                g.Key.CustomerName,
                g.Count(),
                g.Sum(x => x.TotalAmount)
            ))
            .OrderByDescending(c => c.TotalRevenue)
            .Take(5)
            .ToList();

        // Top Products from SalesOrderItems
        var orderItems = await _context.SalesOrderItems
            .Include(oi => oi.Product)
            .Where(oi => oi.SalesOrder.CompanyId == request.CompanyId)
            .ToListAsync(cancellationToken);

        var topProducts = orderItems
            .GroupBy(oi => new { oi.ProductId, Code = oi.Product != null ? oi.Product.Sku : "-", Name = oi.Product != null ? oi.Product.Name : "-" })
            .Select(g => new TopProductDto(
                g.Key.ProductId,
                g.Key.Code,
                g.Key.Name,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.LineTotal)
            ))
            .OrderByDescending(p => p.TotalRevenue)
            .Take(5)
            .ToList();

        var result = new SalesAnalyticsDto(
            totalInvoiced,
            totalCollected,
            totalOutstandingAr,
            totalOrders,
            invoices.Count,
            topCustomers,
            topProducts
        );

        return Result<SalesAnalyticsDto>.Success(result);
    }
}
