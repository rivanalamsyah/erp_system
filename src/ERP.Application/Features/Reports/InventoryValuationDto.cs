namespace ERP.Application.Features.Reports;

public record StockValuationItemDto(
    Guid WarehouseId,
    string WarehouseName,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string UomName,
    decimal PhysicalQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    decimal UnitCost,
    decimal TotalValuation
);

public record InventoryValuationDto(
    decimal GrandTotalValuation,
    decimal GrandTotalQuantity,
    int TotalItemCount,
    List<StockValuationItemDto> Items
);
