namespace ERP.Application.Features.Master.Companies;

public record CompanyDto(
    Guid Id,
    string Code,
    string Name,
    string TaxIdentificationNumber,
    string Address,
    string? Phone,
    string? Email,
    string CurrencyCode,
    DateTime CreatedAtUtc,
    int BranchCount);

