using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Leaves;

[RequirePermission(Permissions.Hr.LeaveView)]
public record GetLeaveRequestsQuery(Guid? EmployeeId = null) : IRequest<Result<List<LeaveRequestDto>>>;

public class GetLeaveRequestsQueryHandler : IRequestHandler<GetLeaveRequestsQuery, Result<List<LeaveRequestDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetLeaveRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<LeaveRequestDto>>> Handle(GetLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.LeaveRequests
            .Include(l => l.Employee)
                .ThenInclude(e => e.Department)
            .Include(l => l.LeaveType)
            .AsNoTracking();

        if (request.EmployeeId.HasValue && request.EmployeeId.Value != Guid.Empty)
        {
            query = query.Where(l => l.EmployeeId == request.EmployeeId.Value);
        }

        var list = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Select(l => new LeaveRequestDto(
                l.Id,
                l.EmployeeId,
                l.Employee.EmployeeNumber,
                l.Employee.FullName,
                l.Employee.Department != null ? l.Employee.Department.Name : "-",
                l.LeaveTypeId,
                l.LeaveType != null ? l.LeaveType.Name : "-",
                l.StartDate,
                l.EndDate,
                l.DaysCount,
                l.Reason,
                l.Status,
                l.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return Result<List<LeaveRequestDto>>.Success(list);
    }
}
