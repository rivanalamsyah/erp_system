namespace ERP.Domain.Entities.Hr;

public enum AttendanceStatus
{
    Present,
    Late,
    Excused,
    Sick,
    Absent
}

public class AttendanceRecord
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly AttendanceDate { get; private set; }
    public DateTime? CheckInTime { get; private set; }
    public DateTime? CheckOutTime { get; private set; }
    public AttendanceStatus Status { get; private set; } = AttendanceStatus.Present;
    public int LateMinutes { get; private set; }
    public string? Notes { get; private set; }

    public Employee Employee { get; private set; } = default!;

    private AttendanceRecord() { }

    public static AttendanceRecord Create(
        Guid employeeId,
        DateOnly attendanceDate,
        DateTime? checkInTime,
        DateTime? checkOutTime,
        AttendanceStatus status,
        int lateMinutes = 0,
        string? notes = null)
    {
        return new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            AttendanceDate = attendanceDate,
            CheckInTime = checkInTime,
            CheckOutTime = checkOutTime,
            Status = status,
            LateMinutes = lateMinutes,
            Notes = notes?.Trim()
        };
    }

    public void RecordCheckOut(DateTime checkOutTime)
    {
        CheckOutTime = checkOutTime;
    }
}

