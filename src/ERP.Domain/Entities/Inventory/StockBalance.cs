using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Inventory;

public class StockBalance
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal PhysicalQuantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal AvailableQuantity => PhysicalQuantity - ReservedQuantity;
    public decimal AverageCost { get; private set; }
    public DateTime LastUpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    public Company Company { get; private set; } = default!;
    public Warehouse Warehouse { get; private set; } = default!;
    public Product Product { get; private set; } = default!;

    private StockBalance() { }

    public static StockBalance Create(Guid companyId, Guid warehouseId, Guid productId)
    {
        return new StockBalance
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            WarehouseId = warehouseId,
            ProductId = productId,
            PhysicalQuantity = 0,
            ReservedQuantity = 0,
            AverageCost = 0,
            LastUpdatedAtUtc = DateTime.UtcNow
        };
    }

    // Inbound Movement: Menambah stok fisik & rekalkulasi Moving Average Cost
    public void AddStock(decimal quantity, decimal unitCost)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Kuantitas masuk harus lebih dari 0.");
        if (unitCost < 0)
            throw new ArgumentOutOfRangeException(nameof(unitCost), "Biaya unit tidak boleh negatif.");

        var totalCurrentValuation = PhysicalQuantity * AverageCost;
        var totalInboundValuation = quantity * unitCost;
        var newTotalQuantity = PhysicalQuantity + quantity;

        PhysicalQuantity = newTotalQuantity;
        AverageCost = newTotalQuantity > 0 ? (totalCurrentValuation + totalInboundValuation) / newTotalQuantity : unitCost;
        LastUpdatedAtUtc = DateTime.UtcNow;
    }

    // Outbound Movement: Mengurangi stok fisik dengan validasi Anti-Stok-Negatif (BR-INV-001)
    public void DeductStock(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Kuantitas keluar harus lebih dari 0.");

        if (PhysicalQuantity - quantity < 0)
        {
            throw new InvalidOperationException(
                $"BR-INV-001: Pelanggaran batas stok negatif. Stok fisik saat ini ({PhysicalQuantity:N2}) tidak mencukupi untuk pengeluaran ({quantity:N2}).");
        }

        PhysicalQuantity -= quantity;
        LastUpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReserveStock(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Kuantitas reservasi harus lebih dari 0.");

        if (AvailableQuantity - quantity < 0)
        {
            throw new InvalidOperationException(
                $"Stok tersedia ({AvailableQuantity:N2}) tidak mencukupi untuk reservasi ({quantity:N2}).");
        }

        ReservedQuantity += quantity;
        LastUpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReleaseReservation(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Kuantitas pelepasan reservasi harus lebih dari 0.");

        ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
        LastUpdatedAtUtc = DateTime.UtcNow;
    }

    public void AdjustPhysicalStock(decimal newPhysicalQuantity, decimal unitCost)
    {
        if (newPhysicalQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(newPhysicalQuantity), "Stok fisik baru tidak boleh negatif.");

        PhysicalQuantity = newPhysicalQuantity;
        if (unitCost > 0)
        {
            AverageCost = unitCost;
        }
        LastUpdatedAtUtc = DateTime.UtcNow;
    }
}

