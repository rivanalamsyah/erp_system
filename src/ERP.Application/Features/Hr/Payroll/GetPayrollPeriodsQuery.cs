using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Payroll;

[RequirePermission(Permissions.Hr.PayrollView)]
public record GetPayrollPeriodsQuery(Guid? CompanyId = null) : IRequest<Result<List<PayrollPeriodDto>>>;

public class GetPayrollPeriodsQueryHandler : IRequestHandler<GetPayrollPeriodsQuery, Result<List<PayrollPeriodDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPayrollPeriodsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<PayrollPeriodDto>>> Handle(GetPayrollPeriodsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PayrollPeriods
            .Include(p => p.Items)
                .ThenInclude(i => i.Employee)
                    .ThenInclude(e => e.Department)
            .AsNoTracking();

        if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CompanyId == request.CompanyId.Value);
        }

        var periods = await query
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(cancellationToken);

        var list = periods.Select(p => new PayrollPeriodDto(
            p.Id,
            p.CompanyId,
            p.PeriodName,
            p.StartDate,
            p.EndDate,
            p.Status,
            p.Items.Sum(i => i.BasicSalary),
            p.Items.Sum(i => i.Allowances),
            p.Items.Sum(i => i.Deductions),
            p.Items.Sum(i => i.IncomeTaxPph21),
            p.Items.Sum(i => i.NetSalary),
            p.Items.Count,
            p.Items.Select(i => new PayrollItemDto(
                i.Id,
                i.EmployeeId,
                i.Employee.EmployeeNumber,
                i.Employee.FullName,
                i.Employee.Department != null ? i.Employee.Department.Name : "-",
                i.BasicSalary,
                i.Allowances,
                i.Deductions,
                i.IncomeTaxPph21,
                i.NetSalary,
                i.IsPaid
            )).ToList()
        )).ToList();

        return Result<List<PayrollPeriodDto>>.Success(list);
    }
}
