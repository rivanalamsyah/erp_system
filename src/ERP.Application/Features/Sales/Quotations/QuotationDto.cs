namespace ERP.Application.Features.Sales.Quotations;

public record QuotationItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string UomCode,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal LineTotal);

public record QuotationDto(
    Guid Id,
    string QuotationNumber,
    Guid CustomerId,
    string CustomerName,
    DateOnly QuotationDate,
    DateOnly ExpirationDate,
    string Status,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<QuotationItemDto> Items);

