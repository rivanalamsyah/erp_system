using ERP.Domain.Common;
using ERP.Domain.Entities.Inventory;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Sales;

public enum DeliveryStatus
{
    Draft,
    Dispatched,
    Delivered,
    Cancelled
}

public class Delivery : BaseEntity<Guid>
{
    public Guid WarehouseId { get; private set; }
    public Guid SalesOrderId { get; private set; }
    public string DeliveryNumber { get; private set; } = string.Empty;
    public DateOnly DeliveryDate { get; private set; }
    public string? TrackingNumber { get; private set; }
    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Draft;

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public Warehouse Warehouse { get; private set; } = default!;
    public SalesOrder SalesOrder { get; private set; } = default!;
    public ICollection<DeliveryItem> Items { get; private set; } = [];

    private Delivery() { }

    public static Delivery Create(
        Guid companyId,
        Guid branchId,
        Guid warehouseId,
        Guid salesOrderId,
        string deliveryNumber,
        DateOnly deliveryDate,
        string? trackingNumber,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryNumber);

        return new Delivery
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            SalesOrderId = salesOrderId,
            DeliveryNumber = deliveryNumber.Trim(),
            DeliveryDate = deliveryDate,
            TrackingNumber = trackingNumber?.Trim(),
            Status = DeliveryStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void AddItem(Guid soItemId, Guid productId, decimal quantityShipped)
    {
        if (Status != DeliveryStatus.Draft)
            throw new InvalidOperationException("Item surat jalan hanya dapat ditambahkan pada status Draft.");
        if (quantityShipped <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantityShipped), "Kuantitas kirim harus lebih dari 0.");

        Items.Add(new DeliveryItem
        {
            Id = Guid.NewGuid(),
            DeliveryId = Id,
            SoItemId = soItemId,
            ProductId = productId,
            QuantityShipped = quantityShipped
        });
    }

    public void Dispatch(Guid updatedByUserId)
    {
        if (Status != DeliveryStatus.Draft)
            throw new InvalidOperationException("Hanya Surat Jalan berstatus Draft yang dapat didispatch.");
        if (Items.Count == 0)
            throw new InvalidOperationException("Surat Jalan harus memuat minimal satu item pengiriman.");

        Status = DeliveryStatus.Dispatched;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkDelivered(Guid updatedByUserId)
    {
        if (Status != DeliveryStatus.Dispatched)
            throw new InvalidOperationException("Hanya Surat Jalan yang telah diberangkatkan (Dispatched) yang dapat ditandai Delivered.");

        Status = DeliveryStatus.Delivered;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class DeliveryItem
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public Guid SoItemId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityShipped { get; set; }

    public Delivery Delivery { get; set; } = default!;
    public SalesOrderItem SalesOrderItem { get; set; } = default!;
    public Product Product { get; set; } = default!;
}
