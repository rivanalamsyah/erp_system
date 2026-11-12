using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Hr;
using ERP.Shared;
using ERP.Shared.Security;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Leaves;

[RequirePermission(Permissions.Hr.LeaveView)]
public record CreateLeaveRequestCommand(
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    int DaysCount,
    string Reason
) : IRequest<Result<Guid>>;

public class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand>
{
    public CreateLeaveRequestCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.DaysCount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
    }
}

public class CreateLeaveRequestCommandHandler : IRequestHandler<CreateLeaveRequestCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateLeaveRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Id == request.LeaveTypeId, cancellationToken);

        if (leaveType == null)
        {
            return Result<Guid>.Failure(Error.NotFound("LeaveType.NotFound", "Tipe cuti tidak ditemukan."));
        }

        // BR-HR-001: Validasi Kuota Cuti Karyawan
        var currentYear = request.StartDate.Year;
        var startOfYear = new DateOnly(currentYear, 1, 1);
        var endOfYear = new DateOnly(currentYear, 12, 31);

        var usedDays = await _context.LeaveRequests
            .Where(l => l.EmployeeId == request.EmployeeId &&
                        l.LeaveTypeId == request.LeaveTypeId &&
                        l.Status != LeaveRequestStatus.Rejected &&
                        l.StartDate >= startOfYear &&
                        l.StartDate <= endOfYear)
            .SumAsync(l => l.DaysCount, cancellationToken);

        if (usedDays + request.DaysCount > leaveType.AnnualAllowanceDays)
        {
            return Result<Guid>.Failure(Error.Validation("Leave.QuotaExceeded",
                $"BR-HR-001: Pengajuan cuti ({request.DaysCount} hari) melebihi sisa kuota cuti tahunan. Sisa kuota: {Math.Max(0, leaveType.AnnualAllowanceDays - usedDays)} hari dari total kuota {leaveType.AnnualAllowanceDays} hari."));
        }

        var leave = LeaveRequest.Create(
            request.EmployeeId,
            request.LeaveTypeId,
            request.StartDate,
            request.EndDate,
            request.DaysCount,
            request.Reason
        );

        _context.LeaveRequests.Add(leave);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(leave.Id);
    }
}
