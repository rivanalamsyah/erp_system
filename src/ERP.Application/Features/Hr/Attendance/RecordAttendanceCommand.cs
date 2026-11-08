using ERP.Application.Common.Interfaces;
using ERP.Domain.Entities.Hr;
using ERP.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Attendance;

public record RecordAttendanceCommand(
    Guid EmployeeId,
    DateOnly AttendanceDate,
    DateTime? CheckInTime,
    DateTime? CheckOutTime,
    AttendanceStatus Status,
    int LateMinutes = 0,
    string? Notes = null
) : IRequest<Result<Guid>>;

public class RecordAttendanceCommandValidator : AbstractValidator<RecordAttendanceCommand>
{
    public RecordAttendanceCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.LateMinutes).GreaterThanOrEqualTo(0);
    }
}

public class RecordAttendanceCommandHandler : IRequestHandler<RecordAttendanceCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public RecordAttendanceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(RecordAttendanceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.AttendanceDate == request.AttendanceDate, cancellationToken);

        if (existing != null)
        {
            if (request.CheckOutTime.HasValue)
            {
                existing.RecordCheckOut(request.CheckOutTime.Value);
                await _context.SaveChangesAsync(cancellationToken);
                return Result<Guid>.Success(existing.Id);
            }
            return Result<Guid>.Failure(Error.Conflict("Attendance.Duplicate", "Presensi karyawan untuk tanggal ini sudah tercatat."));
        }

        var record = AttendanceRecord.Create(
            request.EmployeeId,
            request.AttendanceDate,
            request.CheckInTime,
            request.CheckOutTime,
            request.Status,
            request.LateMinutes,
            request.Notes
        );

        _context.AttendanceRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(record.Id);
    }
}
