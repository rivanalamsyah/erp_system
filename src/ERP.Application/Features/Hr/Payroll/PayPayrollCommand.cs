using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Hr;
using ERP.Shared;
using ERP.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Payroll;

[RequirePermission(Permissions.Hr.PayrollProcess)]
public record PayPayrollCommand(Guid PeriodId) : IRequest<Result<Unit>>;

public class PayPayrollCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IAccountingPostingService accountingPostingService)
    : IRequestHandler<PayPayrollCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(PayPayrollCommand request, CancellationToken cancellationToken)
    {
        var period = await context.PayrollPeriods
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

        var periodCheck = await accountingPostingService.EnsureFiscalPeriodOpenAsync(period.CompanyId, period.EndDate, cancellationToken);
        if (periodCheck.IsFailure)
        {
            return Result<Unit>.Failure(periodCheck.Error);
        }

        var userId = currentUserService.UserId ?? Guid.Empty;
        period.MarkPaid();

        var glResult = await accountingPostingService.PostPayrollJournalAsync(
            period,
            currentUserService.BranchId,
            userId,
            cancellationToken);

        if (glResult.IsFailure)
        {
            return Result<Unit>.Failure(glResult.Error);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
