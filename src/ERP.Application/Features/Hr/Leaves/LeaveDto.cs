using ERP.Domain.Entities.Hr;

namespace ERP.Application.Features.Hr.Leaves;

public record LeaveTypeDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    int AnnualAllowanceDays
);

public record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeFullName,
    string DepartmentName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    int DaysCount,
    string Reason,
    LeaveRequestStatus Status,
    DateTime CreatedAtUtc
);
