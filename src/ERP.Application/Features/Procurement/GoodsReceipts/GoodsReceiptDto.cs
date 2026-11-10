namespace ERP.Application.Features.Procurement.GoodsReceipts;

public record GoodsReceiptItemDto(
    Guid Id,
    Guid PoItemId,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string UomCode,
    decimal QuantityReceived);

public record GoodsReceiptDto(
    Guid Id,
    string ReceiptNumber,
    string DeliveryNoteNumber,
    Guid WarehouseId,
    string WarehouseName,
    Guid PurchaseOrderId,
    string PoNumber,
    string SupplierName,
    DateOnly ReceiptDate,
    string Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<GoodsReceiptItemDto> Items);

