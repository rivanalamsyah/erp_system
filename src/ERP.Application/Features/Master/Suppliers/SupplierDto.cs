namespace ERP.Application.Features.Master.Suppliers;

public record SupplierDto(
    Guid Id,
    Guid CompanyId,
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string Address,
    string? TaxNumber,
    int PaymentTermsDays,
    string? BankAccountName,
    string? BankAccountNumber,
    string? BankName,
    bool IsActive,
    DateTime CreatedAtUtc);

