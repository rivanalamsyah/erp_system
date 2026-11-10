using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Master.Customers;

[RequirePermission(Permissions.Master.CustomerView)]
public record GetCustomersQuery(
    Guid? CompanyId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<CustomerDto>>>;

public class GetCustomersQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetCustomersQuery, Result<PagedResult<CustomerDto>>>
{
    public async Task<Result<PagedResult<CustomerDto>>> Handle(
        GetCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Customers.AsNoTracking();

        if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
        {
            query = query.Where(c => c.CompanyId == request.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.Code.ToLower().Contains(search) ||
                c.Name.ToLower().Contains(search) ||
                (c.TaxNumber != null && c.TaxNumber.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerDto(
                c.Id,
                c.CompanyId,
                c.Code,
                c.Name,
                c.Email,
                c.Phone,
                c.BillingAddress,
                c.ShippingAddress,
                c.TaxNumber,
                c.CreditLimit,
                c.PaymentTermsDays,
                c.IsActive,
                c.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<CustomerDto>>.Success(
            new PagedResult<CustomerDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

