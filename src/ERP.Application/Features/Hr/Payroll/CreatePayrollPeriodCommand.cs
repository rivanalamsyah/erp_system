using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Hr;
using ERP.Shared;
using ERP.Shared.Security;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Payroll;

[RequirePermission(Permissions.Hr.PayrollProcess)]
public record CreatePayrollPeriodCommand(
    Guid CompanyId,
    string PeriodName,
    DateOnly StartDate,
    DateOnly EndDate
) : IRequest<Result<Guid>>;

public class CreatePayrollPeriodCommandValidator : AbstractValidator<CreatePayrollPeriodCommand>
{
    public CreatePayrollPeriodCommandValidator()
    {
        RuleFor(x => x.PeriodName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
    }
}

public class CreatePayrollPeriodCommandHandler : IRequestHandler<CreatePayrollPeriodCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreatePayrollPeriodCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(CreatePayrollPeriodCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.PayrollPeriods
            .AnyAsync(p => p.CompanyId == request.CompanyId && p.PeriodName == request.PeriodName, cancellationToken);

        if (existing)
        {
            return Result<Guid>.Failure(Error.Conflict("Payroll.DuplicatePeriod", $"Periode penggajian '{request.PeriodName}' sudah ada."));
        }

        var userId = _currentUserService.UserId ?? Guid.NewGuid();

        var period = PayrollPeriod.Create(
            request.CompanyId,
            request.PeriodName,
            request.StartDate,
            request.EndDate,
            userId
        );

        _context.PayrollPeriods.Add(period);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(period.Id);
    }
}
