namespace ERP.Application.Features.Inventory.StockTransfers;

public record StockTransferItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string UomCode,
    decimal Quantity);

public record StockTransferDto(
    Guid Id,
    string TransferNumber,
    Guid SourceWarehouseId,
    string SourceWarehouseName,
    Guid DestinationWarehouseId,
    string DestinationWarehouseName,
    DateOnly TransferDate,
    string Status,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<StockTransferItemDto> Items);

