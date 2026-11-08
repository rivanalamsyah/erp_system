using ERP.Application.Common.Interfaces;
using ERP.Domain.Entities.Hr;
using ERP.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Payroll;

public record PayPayrollCommand(Guid PeriodId) : IRequest<Result<Unit>>;

public class PayPayrollCommandHandler : IRequestHandler<PayPayrollCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public PayPayrollCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(PayPayrollCommand request, CancellationToken cancellationToken)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PeriodId, cancellationToken);

        if (period == null)
        {
            return Result<Unit>.Failure(Error.NotFound("Payroll.NotFound", "Periode penggajian tidak ditemukan."));
        }

        if (period.Status != PayrollStatus.Approved)
        {
            return Result<Unit>.Failure(Error.Validation("Payroll.NotApproved", "Hanya periode penggajian yang berstatus Approved yang dapat dibayarkan."));
        }

        period.MarkPaid();
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
