using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Finance.FiscalPeriods;

[RequirePermission(Permissions.Finance.PeriodClose)]
public record GetFiscalPeriodsQuery(int? FiscalYear = null) : IRequest<Result<IReadOnlyList<FiscalPeriodDto>>>;

public class GetFiscalPeriodsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetFiscalPeriodsQuery, Result<IReadOnlyList<FiscalPeriodDto>>>
{
    public async Task<Result<IReadOnlyList<FiscalPeriodDto>>> Handle(
        GetFiscalPeriodsQuery request,
        CancellationToken cancellationToken)
    {
        var year = request.FiscalYear ?? DateTime.UtcNow.Year;

        var periods = await context.FiscalPeriods
            .AsNoTracking()
            .Where(p => p.FiscalYear == year)
            .OrderBy(p => p.PeriodNumber)
            .Select(p => new FiscalPeriodDto(
                p.Id,
                p.FiscalYear,
                p.PeriodNumber,
                p.StartDate,
                p.EndDate,
                p.IsClosed,
                p.ClosedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<FiscalPeriodDto>>.Success(periods);
    }
}

[RequirePermission(Permissions.Finance.PeriodClose)]
public record ToggleFiscalPeriodCommand(Guid Id) : IRequest<Result>;

public class ToggleFiscalPeriodCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<ToggleFiscalPeriodCommand, Result>
{
    public async Task<Result> Handle(
        ToggleFiscalPeriodCommand request,
        CancellationToken cancellationToken)
    {
        var period = await context.FiscalPeriods
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (period == null)
            return Result.Failure(Error.NotFound("Period.NotFound", "Periode fiskal tidak ditemukan."));

        if (period.IsClosed)
        {
            period.Reopen();
        }
        else
        {
            period.Close(currentUserService.UserId ?? Guid.Empty);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

