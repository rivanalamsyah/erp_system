using ERP.Domain.Common;
using ERP.Domain.Entities.Inventory;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Procurement;

public enum GoodsReceiptStatus
{
    Draft,
    Received,
    Cancelled
}

public class GoodsReceipt : BaseEntity<Guid>
{
    public Guid WarehouseId { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public string DeliveryNoteNumber { get; private set; } = string.Empty; // Surat Jalan Vendor
    public DateOnly ReceiptDate { get; private set; }
    public GoodsReceiptStatus Status { get; private set; } = GoodsReceiptStatus.Draft;

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public Warehouse Warehouse { get; private set; } = default!;
    public PurchaseOrder PurchaseOrder { get; private set; } = default!;
    public ICollection<GoodsReceiptItem> Items { get; private set; } = [];

    private GoodsReceipt() { }

    public static GoodsReceipt Create(
        Guid companyId,
        Guid branchId,
        Guid warehouseId,
        Guid purchaseOrderId,
        string receiptNumber,
        string deliveryNoteNumber,
        DateOnly receiptDate,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryNoteNumber);

        return new GoodsReceipt
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            PurchaseOrderId = purchaseOrderId,
            ReceiptNumber = receiptNumber.Trim(),
            DeliveryNoteNumber = deliveryNoteNumber.Trim(),
            ReceiptDate = receiptDate,
            Status = GoodsReceiptStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void AddItem(Guid poItemId, Guid productId, decimal quantityReceived)
    {
        if (Status != GoodsReceiptStatus.Draft)
            throw new InvalidOperationException("Item GRN hanya dapat ditambahkan pada status Draft.");
        if (quantityReceived <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantityReceived), "Kuantitas penerimaan harus lebih dari 0.");

        Items.Add(new GoodsReceiptItem
        {
            Id = Guid.NewGuid(),
            GoodsReceiptId = Id,
            PoItemId = poItemId,
            ProductId = productId,
            QuantityReceived = quantityReceived
        });
    }

    public void MarkReceived(Guid updatedByUserId)
    {
        if (Status != GoodsReceiptStatus.Draft)
            throw new InvalidOperationException("Hanya GRN berstatus Draft yang dapat diproses penerimaan fisiknya.");
        if (Items.Count == 0)
            throw new InvalidOperationException("GRN harus memuat minimal satu item penerimaan barang.");

        Status = GoodsReceiptStatus.Received;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class GoodsReceiptItem
{
    public Guid Id { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public Guid PoItemId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityReceived { get; set; }

    public GoodsReceipt GoodsReceipt { get; set; } = default!;
    public PurchaseOrderItem PurchaseOrderItem { get; set; } = default!;
    public Product Product { get; set; } = default!;
}

