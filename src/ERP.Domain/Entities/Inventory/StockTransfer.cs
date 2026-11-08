using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Inventory;

public enum TransferStatus
{
    Draft,
    Dispatched,
    Received,
    Cancelled
}

public class StockTransfer : BaseEntity<Guid>
{
    public string TransferNumber { get; private set; } = string.Empty;
    public Guid SourceWarehouseId { get; private set; }
    public Guid DestinationWarehouseId { get; private set; }
    public DateOnly TransferDate { get; private set; }
    public TransferStatus Status { get; private set; } = TransferStatus.Draft;
    public string? Notes { get; private set; }

    public Company Company { get; private set; } = default!;
    public Warehouse SourceWarehouse { get; private set; } = default!;
    public Warehouse DestinationWarehouse { get; private set; } = default!;
    public ICollection<StockTransferItem> Items { get; private set; } = [];

    private StockTransfer() { }

    public static StockTransfer Create(
        Guid companyId,
        Guid branchId,
        string transferNumber,
        Guid sourceWarehouseId,
        Guid destinationWarehouseId,
        DateOnly transferDate,
        string? notes,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transferNumber);
        if (sourceWarehouseId == destinationWarehouseId)
            throw new InvalidOperationException("Gudang asal dan gudang tujuan tidak boleh sama.");

        return new StockTransfer
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            TransferNumber = transferNumber.Trim(),
            SourceWarehouseId = sourceWarehouseId,
            DestinationWarehouseId = destinationWarehouseId,
            TransferDate = transferDate,
            Status = TransferStatus.Draft,
            Notes = notes?.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void AddItem(Guid productId, decimal quantity)
    {
        if (Status != TransferStatus.Draft)
            throw new InvalidOperationException("Item transfer hanya dapat diubah saat berstatus Draft.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Kuantitas transfer harus lebih besar dari 0.");

        Items.Add(new StockTransferItem
        {
            Id = Guid.NewGuid(),
            TransferId = Id,
            ProductId = productId,
            Quantity = quantity
        });
    }

    public void Dispatch(Guid updatedByUserId)
    {
        if (Status != TransferStatus.Draft)
            throw new InvalidOperationException("Hanya transfer berstatus Draft yang dapat diberangkatkan (Dispatch).");
        if (Items.Count == 0)
            throw new InvalidOperationException("Transfer harus memiliki minimal satu item.");

        Status = TransferStatus.Dispatched;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Receive(Guid updatedByUserId)
    {
        if (Status != TransferStatus.Dispatched)
            throw new InvalidOperationException("Hanya transfer berstatus Dispatched yang dapat diterima (Receive).");

        Status = TransferStatus.Received;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(Guid updatedByUserId)
    {
        if (Status == TransferStatus.Received)
            throw new InvalidOperationException("Transfer yang sudah diterima tidak dapat dibatalkan.");

        Status = TransferStatus.Cancelled;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class StockTransferItem
{
    public Guid Id { get; set; }
    public Guid TransferId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }

    public StockTransfer Transfer { get; set; } = default!;
    public Product Product { get; set; } = default!;
}

