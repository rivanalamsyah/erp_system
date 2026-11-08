using ERP.Application.Common.Interfaces;
using ERP.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Employees;

public record GetEmployeesQuery(Guid? DepartmentId = null, string? SearchTerm = null) : IRequest<Result<List<EmployeeDto>>>;

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, Result<List<EmployeeDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEmployeesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<EmployeeDto>>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Branch)
            .AsNoTracking();

        if (request.DepartmentId.HasValue && request.DepartmentId.Value != Guid.Empty)
        {
            query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(e => e.FullName.ToLower().Contains(term) ||
                                     e.EmployeeNumber.ToLower().Contains(term) ||
                                     e.IdentityCardNumber.ToLower().Contains(term));
        }

        var list = await query
            .OrderBy(e => e.FullName)
            .Select(e => new EmployeeDto(
                e.Id,
                e.CompanyId,
                e.BranchId,
                e.Branch != null ? e.Branch.Name : "-",
                e.DepartmentId,
                e.Department != null ? e.Department.Name : "-",
                e.EmployeeNumber,
                e.FullName,
                e.IdentityCardNumber,
                e.BirthDate,
                e.HireDate,
                e.EmploymentStatus,
                e.BasicSalary,
                e.IsActive
            ))
            .ToListAsync(cancellationToken);

        return Result<List<EmployeeDto>>.Success(list);
    }
}
