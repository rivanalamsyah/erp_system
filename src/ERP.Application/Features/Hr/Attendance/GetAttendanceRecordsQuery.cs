using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Attendance;

[RequirePermission(Permissions.Hr.AttendanceView)]
public record GetAttendanceRecordsQuery(DateOnly? Date = null, Guid? EmployeeId = null) : IRequest<Result<List<AttendanceRecordDto>>>;

public class GetAttendanceRecordsQueryHandler : IRequestHandler<GetAttendanceRecordsQuery, Result<List<AttendanceRecordDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAttendanceRecordsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AttendanceRecordDto>>> Handle(GetAttendanceRecordsQuery request, CancellationToken cancellationToken)
    {
        var targetDate = request.Date ?? DateOnly.FromDateTime(DateTime.Today);

        var query = _context.AttendanceRecords
            .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
            .AsNoTracking()
            .Where(a => a.AttendanceDate == targetDate);

        if (request.EmployeeId.HasValue && request.EmployeeId.Value != Guid.Empty)
        {
            query = query.Where(a => a.EmployeeId == request.EmployeeId.Value);
        }

        var list = await query
            .OrderBy(a => a.Employee.FullName)
            .Select(a => new AttendanceRecordDto(
                a.Id,
                a.EmployeeId,
                a.Employee.EmployeeNumber,
                a.Employee.FullName,
                a.Employee.Department != null ? a.Employee.Department.Name : "-",
                a.AttendanceDate,
                a.CheckInTime,
                a.CheckOutTime,
                a.Status,
                a.LateMinutes,
                a.Notes
            ))
            .ToListAsync(cancellationToken);

        return Result<List<AttendanceRecordDto>>.Success(list);
    }
}
