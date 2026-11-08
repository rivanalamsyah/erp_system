using ERP.Domain.Entities.Hr;

namespace ERP.Application.Features.Hr.Attendance;

public record AttendanceRecordDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeFullName,
    string DepartmentName,
    DateOnly AttendanceDate,
    DateTime? CheckInTime,
    DateTime? CheckOutTime,
    AttendanceStatus Status,
    int LateMinutes,
    string? Notes
);
