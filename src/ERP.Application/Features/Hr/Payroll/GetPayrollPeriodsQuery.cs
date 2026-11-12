using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Payroll;

[RequirePermission(Permissions.Hr.PayrollView)]
public record GetPayrollPeriodsQuery(Guid? CompanyId = null) : IRequest<Result<List<PayrollPeriodDto>>>;

public class GetPayrollPeriodsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetPayrollPeriodsQuery, Result<List<PayrollPeriodDto>>>
{
    public async Task<Result<List<PayrollPeriodDto>>> Handle(GetPayrollPeriodsQuery request, CancellationToken cancellationToken)
    {
        var companyId = request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty
            ? request.CompanyId.Value
            : currentUserService.CompanyId;

        var query = context.PayrollPeriods
            .AsNoTracking();

        if (companyId != Guid.Empty)
        {
            query = query.Where(p => p.CompanyId == companyId);
        }

        var list = await query
            .OrderByDescending(p => p.StartDate)
            .Select(p => new PayrollPeriodDto(
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
            ))
            .ToListAsync(cancellationToken);

        return Result<List<PayrollPeriodDto>>.Success(list);
    }
}
