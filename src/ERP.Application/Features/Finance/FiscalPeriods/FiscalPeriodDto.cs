namespace ERP.Application.Features.Finance.FiscalPeriods;

public record FiscalPeriodDto(
    Guid Id,
    int FiscalYear,
    int PeriodNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsClosed,
    DateTime? ClosedAtUtc);
