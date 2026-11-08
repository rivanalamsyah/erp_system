namespace ERP.Application.Features.Inventory.StockBalances;

public record StockBalanceDto(
    Guid Id,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string UomCode,
    decimal PhysicalQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    decimal AverageCost,
    decimal TotalValuation,
    DateTime LastUpdatedAtUtc);

public record StockMovementDto(
    Guid Id,
    string MovementType,
    decimal Quantity,
    string Direction,
    decimal UnitCost,
    decimal TotalCost,
    string ReferenceNumber,
    DateTime MovementDateUtc);

