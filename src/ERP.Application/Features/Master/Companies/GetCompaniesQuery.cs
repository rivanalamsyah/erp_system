using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared.Common;
using ERP.Shared.Security;

namespace ERP.Application.Features.Master.Companies;

[RequirePermission(Permissions.Master.CompanyView)]
public record GetCompaniesQuery(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<CompanyDto>>>;

public class GetCompaniesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetCompaniesQuery, Result<PagedResult<CompanyDto>>>
{
    public async Task<Result<PagedResult<CompanyDto>>> Handle(
        GetCompaniesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Companies
            .Include(c => c.Branches)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.Code.ToLower().Contains(search) ||
                c.Name.ToLower().Contains(search) ||
                c.TaxIdentificationNumber.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Code)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CompanyDto(
                c.Id,
                c.Code,
                c.Name,
                c.TaxIdentificationNumber,
                c.Address,
                c.Phone,
                c.Email,
                c.CurrencyCode,
                c.CreatedAtUtc,
                c.Branches.Count(b => !b.IsDeleted)))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<CompanyDto>>.Success(
            new PagedResult<CompanyDto>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

