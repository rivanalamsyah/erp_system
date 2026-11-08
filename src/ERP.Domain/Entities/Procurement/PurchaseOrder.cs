using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Procurement;

public enum PurchaseOrderStatus
{
    Draft,
    PendingApproval,
    Approved,
    PartiallyReceived,
    FullyReceived,
    Billed,
    Closed,
    Cancelled
}

public class PurchaseOrder : BaseEntity<Guid>
{
    public Guid SupplierId { get; private set; }
    public string PoNumber { get; private set; } = string.Empty;
    public DateOnly OrderDate { get; private set; }
    public DateOnly? ExpectedDeliveryDate { get; private set; }
    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.Draft;
    public decimal SubtotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public Supplier Supplier { get; private set; } = default!;
    public ICollection<PurchaseOrderItem> Items { get; private set; } = [];

    private PurchaseOrder() { }

    public static PurchaseOrder Create(
        Guid companyId,
        Guid branchId,
        Guid supplierId,
        string poNumber,
        DateOnly orderDate,
        DateOnly? expectedDeliveryDate,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(poNumber);

        return new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            SupplierId = supplierId,
            PoNumber = poNumber.Trim(),
            OrderDate = orderDate,
            ExpectedDeliveryDate = expectedDeliveryDate,
            Status = PurchaseOrderStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void AddItem(Guid productId, decimal quantity, decimal unitPrice, decimal discountRate = 0)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Item PO hanya dapat ditambahkan pada status Draft.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Kuantitas PO harus lebih besar dari 0.");
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Harga per unit tidak boleh negatif.");

        var gross = quantity * unitPrice;
        var discount = gross * (discountRate / 100m);
        var lineTotal = Math.Round(gross - discount, 2);

        Items.Add(new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = Id,
            ProductId = productId,
            Quantity = quantity,
            ReceivedQuantity = 0,
            UnitPrice = unitPrice,
            DiscountRate = discountRate,
            LineTotal = lineTotal
        });

        RecalculateTotals();
    }

    public void RecalculateTotals(decimal taxRate = 0.11m) // PPN 11% standard
    {
        SubtotalAmount = Items.Sum(i => i.LineTotal);
        TaxAmount = Math.Round(SubtotalAmount * taxRate, 2);
        TotalAmount = SubtotalAmount + TaxAmount;
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status != PurchaseOrderStatus.Draft && Status != PurchaseOrderStatus.PendingApproval)
            throw new InvalidOperationException("Hanya PO Draft atau PendingApproval yang dapat disetujui.");
        if (Items.Count == 0)
            throw new InvalidOperationException("PO harus memuat minimal satu item produk.");

        Status = PurchaseOrderStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        UpdatedByUserId = approvedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordReceipt(Guid poItemId, decimal receivedQty)
    {
        var item = Items.FirstOrDefault(i => i.Id == poItemId);
        if (item == null) throw new InvalidOperationException("Item PO tidak ditemukan.");

        item.ReceivedQuantity += receivedQty;

        if (Items.All(i => i.ReceivedQuantity >= i.Quantity))
        {
            Status = PurchaseOrderStatus.FullyReceived;
        }
        else if (Items.Any(i => i.ReceivedQuantity > 0))
        {
            Status = PurchaseOrderStatus.PartiallyReceived;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsBilled()
    {
        Status = PurchaseOrderStatus.Billed;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class PurchaseOrderItem
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal LineTotal { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = default!;
    public Product Product { get; set; } = default!;
}

