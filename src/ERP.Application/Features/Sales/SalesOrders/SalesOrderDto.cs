namespace ERP.Application.Features.Sales.SalesOrders;

public record SalesOrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string UomCode,
    decimal Quantity,
    decimal ShippedQuantity,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal LineTotal);

public record SalesOrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    Guid? QuotationId,
    string? QuotationNumber,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    string Status,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<SalesOrderItemDto> Items);
