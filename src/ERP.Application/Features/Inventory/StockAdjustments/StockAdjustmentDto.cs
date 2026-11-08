namespace ERP.Application.Features.Inventory.StockAdjustments;

public record StockAdjustmentItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string UomCode,
    decimal SystemQuantity,
    decimal PhysicalQuantity,
    decimal DifferenceQuantity,
    decimal UnitCost);

public record StockAdjustmentDto(
    Guid Id,
    string AdjustmentNumber,
    Guid WarehouseId,
    string WarehouseName,
    DateOnly AdjustmentDate,
    string Status,
    string Reason,
    DateTime CreatedAtUtc,
    IReadOnlyList<StockAdjustmentItemDto> Items);

