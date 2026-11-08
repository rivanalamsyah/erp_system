namespace ERP.Application.Features.Master.Customers;

public record CustomerDto(
    Guid Id,
    Guid CompanyId,
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string BillingAddress,
    string? ShippingAddress,
    string? TaxNumber,
    decimal CreditLimit,
    int PaymentTermsDays,
    bool IsActive,
    DateTime CreatedAtUtc);

