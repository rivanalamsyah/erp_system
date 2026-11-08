using ERP.Application.Common.Interfaces;
using ERP.Domain.Entities.Hr;
using ERP.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Payroll;

public record CalculatePayrollCommand(Guid PeriodId) : IRequest<Result<Unit>>;

public class CalculatePayrollCommandHandler : IRequestHandler<CalculatePayrollCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public CalculatePayrollCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(CalculatePayrollCommand request, CancellationToken cancellationToken)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PeriodId, cancellationToken);

        if (period == null)
        {
            return Result<Unit>.Failure(Error.NotFound("Payroll.NotFound", "Periode penggajian tidak ditemukan."));
        }

        if (period.Status != PayrollStatus.Draft)
        {
            return Result<Unit>.Failure(Error.Conflict("Payroll.NotDraft", "Hanya periode berstatus Draft yang dapat dikalkulasi ulang."));
        }

        // Get active employees for this company
        var employees = await _context.Employees
            .Where(e => e.CompanyId == period.CompanyId && e.IsActive)
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
        {
            return Result<Unit>.Failure(Error.NotFound("Payroll.NoEmployees", "Tidak ada karyawan aktif untuk dikalkulasi pada perusahaan ini."));
        }

        // Remove existing draft items if recalculating
        if (period.Items.Count > 0)
        {
            _context.PayrollItems.RemoveRange(period.Items);
            period.Items.Clear();
        }

        foreach (var emp in employees)
        {
            // Standard allowance (e.g. 10% of basic salary)
            var allowance = Math.Round(emp.BasicSalary * 0.10m, 2);
            var gross = emp.BasicSalary + allowance;

            // PPh 21 Indonesian standard simple monthly tier
            decimal pph21 = 0;
            if (gross > 4500000m)
            {
                pph21 = Math.Round((gross - 4500000m) * 0.05m, 2);
            }

            // Fixed BPJS / standard deduction
            var deduction = Math.Round(emp.BasicSalary * 0.01m, 2);

            try
            {
                period.AddItem(emp.Id, emp.BasicSalary, allowance, deduction, pph21);
            }
            catch (Exception ex)
            {
                return Result<Unit>.Failure(Error.Failure("Payroll.CalculationFailed", $"Gagal menghitung gaji untuk {emp.FullName}: {ex.Message}"));
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<Unit>.Success(Unit.Value);
    }
}
