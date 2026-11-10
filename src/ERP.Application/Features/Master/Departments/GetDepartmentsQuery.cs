using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared.Common;
using ERP.Shared.Security;

namespace ERP.Application.Features.Master.Departments;

[RequirePermission(Permissions.Master.DepartmentView)]
public record GetDepartmentsQuery(
    Guid? CompanyId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<DepartmentDto>>>;

public class GetDepartmentsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetDepartmentsQuery, Result<PagedResult<DepartmentDto>>>
{
    public async Task<Result<PagedResult<DepartmentDto>>> Handle(
        GetDepartmentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Departments
            .Include(d => d.Company)
            .AsNoTracking();

        if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
        {
            query = query.Where(d => d.CompanyId == request.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(d => d.Code.ToLower().Contains(search) || d.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(d => d.Code)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DepartmentDto(
                d.Id,
                d.CompanyId,
                d.Company.Name,
                d.Code,
                d.Name,
                d.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<DepartmentDto>>.Success(new PagedResult<DepartmentDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize));
    }
}

