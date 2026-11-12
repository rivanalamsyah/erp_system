using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Sales;
using ERP.Shared;
using ERP.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Reports;

[RequirePermission(Permissions.Reports.FinancialView)]
public record GetSalesAnalyticsQuery(Guid CompanyId, DateOnly? StartDate = null, DateOnly? EndDate = null) : IRequest<Result<SalesAnalyticsDto>>;

public class GetSalesAnalyticsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetSalesAnalyticsQuery, Result<SalesAnalyticsDto>>
{
    public async Task<Result<SalesAnalyticsDto>> Handle(GetSalesAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var companyId = request.CompanyId != Guid.Empty ? request.CompanyId : currentUserService.CompanyId;

        var invQuery = context.SalesInvoices
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.Status != SalesInvoiceStatus.Cancelled);

        if (request.StartDate.HasValue)
        {
            invQuery = invQuery.Where(i => i.InvoiceDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            invQuery = invQuery.Where(i => i.InvoiceDate <= request.EndDate.Value);
        }

        var totalInvoices = await invQuery.CountAsync(cancellationToken);
        var totalInvoiced = totalInvoices > 0 ? await invQuery.SumAsync(i => i.TotalAmount, cancellationToken) : 0m;
        var totalCollected = totalInvoices > 0 ? await invQuery.SumAsync(i => i.PaidAmount, cancellationToken) : 0m;
        var totalOutstandingAr = totalInvoiced - totalCollected;

        var soQuery = context.SalesOrders
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.Status != SalesOrderStatus.Cancelled);

        if (request.StartDate.HasValue)
        {
            soQuery = soQuery.Where(s => s.OrderDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            soQuery = soQuery.Where(s => s.OrderDate <= request.EndDate.Value);
        }

        var totalOrders = await soQuery.CountAsync(cancellationToken);

        // Top 5 Customers aggregated directly in SQL
        var topCustomers = await invQuery
            .GroupBy(i => new { i.CustomerId, CustomerName = i.Customer.Name })
            .Select(g => new TopCustomerDto(
                g.Key.CustomerId,
                g.Key.CustomerName,
                g.Count(),
                g.Sum(x => x.TotalAmount)
            ))
            .OrderByDescending(c => c.TotalRevenue)
            .Take(5)
            .ToListAsync(cancellationToken);

        // Top 5 Products aggregated directly in SQL
        var orderItemsQuery = context.SalesOrderItems
            .AsNoTracking()
            .Where(oi => oi.SalesOrder.CompanyId == companyId && oi.SalesOrder.Status != SalesOrderStatus.Cancelled);

        if (request.StartDate.HasValue)
        {
            orderItemsQuery = orderItemsQuery.Where(oi => oi.SalesOrder.OrderDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            orderItemsQuery = orderItemsQuery.Where(oi => oi.SalesOrder.OrderDate <= request.EndDate.Value);
        }

        var topProducts = await orderItemsQuery
            .GroupBy(oi => new { oi.ProductId, Code = oi.Product.Sku, Name = oi.Product.Name })
            .Select(g => new TopProductDto(
                g.Key.ProductId,
                g.Key.Code,
                g.Key.Name,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.LineTotal)
            ))
            .OrderByDescending(p => p.TotalRevenue)
            .Take(5)
            .ToListAsync(cancellationToken);

        var result = new SalesAnalyticsDto(
            totalInvoiced,
            totalCollected,
            totalOutstandingAr,
            totalOrders,
            totalInvoices,
            topCustomers,
            topProducts
        );

        return Result<SalesAnalyticsDto>.Success(result);
    }
}
