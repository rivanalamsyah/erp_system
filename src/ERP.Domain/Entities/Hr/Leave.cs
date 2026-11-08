using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Hr;

public enum LeaveRequestStatus
{
    Pending,
    Approved,
    Rejected
}

public class LeaveType : BaseEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public int AnnualAllowanceDays { get; private set; } = 12;

    public Company Company { get; private set; } = default!;

    private LeaveType() { }

    public static LeaveType Create(Guid companyId, string name, int annualAllowanceDays, Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new LeaveType
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name.Trim(),
            AnnualAllowanceDays = annualAllowanceDays,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }
}

public class LeaveRequest
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int DaysCount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public LeaveRequestStatus Status { get; private set; } = LeaveRequestStatus.Pending;
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public Employee Employee { get; private set; } = default!;
    public LeaveType LeaveType { get; private set; } = default!;

    private LeaveRequest() { }

    public static LeaveRequest Create(
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        int daysCount,
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (daysCount <= 0) throw new ArgumentOutOfRangeException(nameof(daysCount), "Jumlah hari cuti harus lebih besar dari 0.");

        return new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            DaysCount = daysCount,
            Reason = reason.Trim(),
            Status = LeaveRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Approve(Guid approvedByUserId)
    {
        Status = LeaveRequestStatus.Approved;
        ApprovedByUserId = approvedByUserId;
    }

    public void Reject(Guid rejectedByUserId)
    {
        Status = LeaveRequestStatus.Rejected;
        ApprovedByUserId = rejectedByUserId;
    }
}
