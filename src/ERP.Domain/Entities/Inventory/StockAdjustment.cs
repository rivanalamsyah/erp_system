using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Inventory;

public enum AdjustmentStatus
{
    Draft,
    Approved,
    Rejected
}

public class StockAdjustment : BaseEntity<Guid>
{
    public string AdjustmentNumber { get; private set; } = string.Empty;
    public Guid WarehouseId { get; private set; }
    public DateOnly AdjustmentDate { get; private set; }
    public AdjustmentStatus Status { get; private set; } = AdjustmentStatus.Draft;
    public string Reason { get; private set; } = string.Empty;
    public Guid? ApprovedByUserId { get; private set; }

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public Warehouse Warehouse { get; private set; } = default!;
    public ICollection<StockAdjustmentItem> Items { get; private set; } = [];

    private StockAdjustment() { }

    public static StockAdjustment Create(
        Guid companyId,
        Guid branchId,
        Guid warehouseId,
        string adjustmentNumber,
        DateOnly adjustmentDate,
        string reason,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adjustmentNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new StockAdjustment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            AdjustmentNumber = adjustmentNumber.Trim(),
            AdjustmentDate = adjustmentDate,
            Status = AdjustmentStatus.Draft,
            Reason = reason.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void AddItem(Guid productId, decimal systemQty, decimal physicalQty, decimal unitCost)
    {
        if (Status != AdjustmentStatus.Draft)
            throw new InvalidOperationException("Item opname hanya dapat ditambahkan pada status Draft.");

        Items.Add(new StockAdjustmentItem
        {
            Id = Guid.NewGuid(),
            AdjustmentId = Id,
            ProductId = productId,
            SystemQuantity = systemQty,
            PhysicalQuantity = physicalQty,
            DifferenceQuantity = physicalQty - systemQty,
            UnitCost = unitCost
        });
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status != AdjustmentStatus.Draft)
            throw new InvalidOperationException("Hanya penyesuaian berstatus Draft yang dapat disetujui.");
        if (Items.Count == 0)
            throw new InvalidOperationException("Penyesuaian stok harus memuat minimal satu item produk.");

        Status = AdjustmentStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        UpdatedByUserId = approvedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reject(Guid rejectedByUserId)
    {
        if (Status != AdjustmentStatus.Draft)
            throw new InvalidOperationException("Hanya penyesuaian berstatus Draft yang dapat ditolak.");

        Status = AdjustmentStatus.Rejected;
        UpdatedByUserId = rejectedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class StockAdjustmentItem
{
    public Guid Id { get; set; }
    public Guid AdjustmentId { get; set; }
    public Guid ProductId { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal PhysicalQuantity { get; set; }
    public decimal DifferenceQuantity { get; set; }
    public decimal UnitCost { get; set; }

    public StockAdjustment Adjustment { get; set; } = default!;
    public Product Product { get; set; } = default!;
}

