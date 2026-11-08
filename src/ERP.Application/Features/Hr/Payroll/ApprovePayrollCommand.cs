using ERP.Application.Common.Interfaces;
using ERP.Domain.Entities.Hr;
using ERP.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Payroll;

public record ApprovePayrollCommand(Guid PeriodId) : IRequest<Result<Unit>>;

public class ApprovePayrollCommandHandler : IRequestHandler<ApprovePayrollCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ApprovePayrollCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Unit>> Handle(ApprovePayrollCommand request, CancellationToken cancellationToken)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PeriodId, cancellationToken);

        if (period == null)
        {
            return Result<Unit>.Failure(Error.NotFound("Payroll.NotFound", "Periode penggajian tidak ditemukan."));
        }

        if (period.Items.Count == 0)
        {
            return Result<Unit>.Failure(Error.Validation("Payroll.Empty", "Periode penggajian belum dikalkulasi / tidak memiliki data gaji."));
        }

        if (period.Status == PayrollStatus.Approved || period.Status == PayrollStatus.Paid)
        {
            return Result<Unit>.Failure(Error.Conflict("Payroll.AlreadyApproved", "Periode penggajian sudah disetujui sebelumnya."));
        }

        var userId = _currentUserService.UserId ?? Guid.NewGuid();
        period.Approve(userId);

        await _context.SaveChangesAsync(cancellationToken);
        return Result<Unit>.Success(Unit.Value);
    }
}
