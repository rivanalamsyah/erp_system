namespace ERP.Application.Features.Sales.Deliveries;

public record DeliveryItemDto(
    Guid Id,
    Guid SoItemId,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string UomCode,
    decimal QuantityShipped);

public record DeliveryDto(
    Guid Id,
    string DeliveryNumber,
    Guid WarehouseId,
    string WarehouseName,
    Guid SalesOrderId,
    string OrderNumber,
    string CustomerName,
    DateOnly DeliveryDate,
    string? TrackingNumber,
    string Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<DeliveryItemDto> Items);
