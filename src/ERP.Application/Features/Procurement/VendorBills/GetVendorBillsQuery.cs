using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Procurement.VendorBills;

[RequirePermission(Permissions.Procurement.BillView)]
public record GetVendorBillsQuery(
    Guid? SupplierId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<VendorBillDto>>;

public class GetVendorBillsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetVendorBillsQuery, PagedResult<VendorBillDto>>
{
    public async Task<PagedResult<VendorBillDto>> Handle(
        GetVendorBillsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.VendorBills
            .Include(v => v.Supplier)
            .Include(v => v.PurchaseOrder)
            .AsNoTracking();

        if (request.SupplierId.HasValue && request.SupplierId != Guid.Empty)
        {
            query = query.Where(v => v.SupplierId == request.SupplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(v => v.BillNumber.ToLower().Contains(search)
                || v.VendorInvoiceNumber.ToLower().Contains(search)
                || v.PurchaseOrder.PoNumber.ToLower().Contains(search)
                || v.Supplier.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(v => v.BillDate)
            .ThenByDescending(v => v.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new VendorBillDto(
                v.Id,
                v.BillNumber,
                v.VendorInvoiceNumber,
                v.SupplierId,
                v.Supplier.Name,
                v.PurchaseOrderId,
                v.PurchaseOrder.PoNumber,
                v.BillDate,
                v.DueDate,
                v.TotalAmount,
                v.PaidAmount,
                v.RemainingAmount,
                v.Status.ToString(),
                v.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<VendorBillDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
