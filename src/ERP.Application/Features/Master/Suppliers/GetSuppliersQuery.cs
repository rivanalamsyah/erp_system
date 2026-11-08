using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Shared;

namespace ERP.Application.Features.Master.Suppliers;

public record GetSuppliersQuery(
    Guid? CompanyId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<SupplierDto>>>;

public class GetSuppliersQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetSuppliersQuery, Result<PagedResult<SupplierDto>>>
{
    public async Task<Result<PagedResult<SupplierDto>>> Handle(
        GetSuppliersQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Suppliers.AsNoTracking();

        if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
        {
            query = query.Where(s => s.CompanyId == request.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(s =>
                s.Code.ToLower().Contains(search) ||
                s.Name.ToLower().Contains(search) ||
                (s.TaxNumber != null && s.TaxNumber.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SupplierDto(
                s.Id,
                s.CompanyId,
                s.Code,
                s.Name,
                s.Email,
                s.Phone,
                s.Address,
                s.TaxNumber,
                s.PaymentTermsDays,
                s.BankAccountName,
                s.BankAccountNumber,
                s.BankName,
                s.IsActive,
                s.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<SupplierDto>>.Success(
            new PagedResult<SupplierDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

