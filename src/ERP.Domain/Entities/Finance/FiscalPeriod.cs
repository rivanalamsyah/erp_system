using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Finance;

public class FiscalPeriod
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public int FiscalYear { get; private set; }
    public int PeriodNumber { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsClosed { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? ClosedByUserId { get; private set; }

    public Company Company { get; private set; } = default!;

    private FiscalPeriod() { }

    public static FiscalPeriod Create(
        Guid companyId,
        int fiscalYear,
        int periodNumber,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (periodNumber < 1 || periodNumber > 12)
            throw new ArgumentOutOfRangeException(nameof(periodNumber), "Periode fiskal harus antara bulan 1 dan 12.");

        return new FiscalPeriod
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FiscalYear = fiscalYear,
            PeriodNumber = periodNumber,
            StartDate = startDate,
            EndDate = endDate,
            IsClosed = false
        };
    }

    public void Close(Guid closedByUserId)
    {
        IsClosed = true;
        ClosedAtUtc = DateTime.UtcNow;
        ClosedByUserId = closedByUserId;
    }

    public void Reopen()
    {
        IsClosed = false;
        ClosedAtUtc = null;
        ClosedByUserId = null;
    }
}

