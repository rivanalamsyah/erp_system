namespace ERP.Application.Features.Finance.Payments;

public record PaymentAllocationDto(
    Guid Id,
    Guid InvoiceId,
    decimal AllocatedAmount);

public record PaymentDto(
    Guid Id,
    string PaymentNumber,
    DateOnly PaymentDate,
    string PaymentType,
    Guid PartnerId,
    string PartnerName,
    Guid CashBankAccountId,
    string CashBankAccountName,
    string PaymentMethod,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<PaymentAllocationDto> Allocations);
