using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Finance;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Finance.Payments;

[RequirePermission(Permissions.Finance.PaymentProcess)]
public record GetPaymentsQuery(
    string? PaymentType = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<PaymentDto>>>;

public class GetPaymentsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetPaymentsQuery, Result<PagedResult<PaymentDto>>>
{
    public async Task<Result<PagedResult<PaymentDto>>> Handle(
        GetPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Payments
            .Include(p => p.CashBankAccount)
            .Include(p => p.Allocations)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.PaymentType))
        {
            var typeSearch = request.PaymentType.Trim();
            query = query.Where(p => p.PaymentType.ToString() == typeSearch);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(p => p.PaymentNumber.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rawItems = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var customerIds = rawItems.Where(p => p.PaymentType == PaymentType.CustomerReceipt).Select(p => p.PartnerId).Distinct().ToList();
        var supplierIds = rawItems.Where(p => p.PaymentType == PaymentType.VendorPayment).Select(p => p.PartnerId).Distinct().ToList();

        var customers = await context.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var suppliers = await context.Suppliers
            .AsNoTracking()
            .Where(s => supplierIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var items = rawItems.Select(p =>
        {
            var partnerName = p.PaymentType == PaymentType.CustomerReceipt
                ? (customers.TryGetValue(p.PartnerId, out var cName) ? cName : "Pelanggan")
                : (suppliers.TryGetValue(p.PartnerId, out var sName) ? sName : "Pemasok");

            return new PaymentDto(
                p.Id,
                p.PaymentNumber,
                p.PaymentDate,
                p.PaymentType.ToString(),
                p.PartnerId,
                partnerName,
                p.CashBankAccountId,
                p.CashBankAccount.Name,
                p.PaymentMethod.ToString(),
                p.TotalAmount,
                p.Status.ToString(),
                p.CreatedAtUtc,
                p.Allocations.Select(a => new PaymentAllocationDto(a.Id, a.InvoiceId, a.AllocatedAmount)).ToList()
            );
        }).ToList();

        return Result<PagedResult<PaymentDto>>.Success(
            new PagedResult<PaymentDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}
