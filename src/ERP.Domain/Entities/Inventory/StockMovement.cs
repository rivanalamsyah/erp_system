using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Inventory;

public enum MovementType
{
    PurchaseReceipt,
    SalesDelivery,
    TransferIn,
    TransferOut,
    AdjustmentPositive,
    AdjustmentNegative
}

public class StockMovement
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid ProductId { get; private set; }
    public MovementType MovementType { get; private set; }
    public decimal Quantity { get; private set; }
    public string Direction { get; private set; } = "IN"; // 'IN' or 'OUT'
    public decimal UnitCost { get; private set; }
    public decimal TotalCost { get; private set; }
    public string ReferenceNumber { get; private set; } = string.Empty;
    public DateTime MovementDateUtc { get; private set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; private set; }

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public Warehouse Warehouse { get; private set; } = default!;
    public Product Product { get; private set; } = default!;

    private StockMovement() { }

    public static StockMovement Create(
        Guid companyId,
        Guid branchId,
        Guid warehouseId,
        Guid productId,
        MovementType movementType,
        decimal quantity,
        string direction,
        decimal unitCost,
        string referenceNumber,
        Guid createdByUserId)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Kuantitas mutasi harus lebih besar dari 0.");
        if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost), "Biaya per unit tidak boleh negatif.");
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceNumber);

        var dir = direction.Trim().ToUpperInvariant();
        if (dir != "IN" && dir != "OUT")
            throw new ArgumentException("Arah mutasi stok hanya boleh 'IN' atau 'OUT'.", nameof(direction));

        return new StockMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            ProductId = productId,
            MovementType = movementType,
            Quantity = quantity,
            Direction = dir,
            UnitCost = unitCost,
            TotalCost = Math.Round(quantity * unitCost, 2),
            ReferenceNumber = referenceNumber.Trim(),
            MovementDateUtc = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };
    }
}

