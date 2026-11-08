namespace ERP.Application.Features.Procurement.PurchaseRequests;

public record PurchaseRequestItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string UomCode,
    decimal Quantity,
    decimal EstimatedUnitPrice)
{
    public decimal EstimatedTotal => Quantity * EstimatedUnitPrice;
}

public record PurchaseRequestDto(
    Guid Id,
    string RequestNumber,
    DateOnly RequestDate,
    DateOnly RequiredDate,
    string Status,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<PurchaseRequestItemDto> Items)
{
    public decimal TotalEstimatedAmount => Items.Sum(i => i.EstimatedTotal);
}
