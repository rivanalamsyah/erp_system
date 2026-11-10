namespace ERP.Application.Features.Procurement.PurchaseOrders;

public record PurchaseOrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string UomCode,
    decimal Quantity,
    decimal ReceivedQuantity,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal LineTotal);

public record PurchaseOrderDto(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    string Status,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<PurchaseOrderItemDto> Items);

