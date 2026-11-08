using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Sales;

public enum SalesOrderStatus
{
    Draft,
    Confirmed,
    PartiallyShipped,
    FullyShipped,
    Invoiced,
    Closed,
    Cancelled
}

public class SalesOrder : BaseEntity<Guid>
{
    public Guid CustomerId { get; private set; }
    public Guid? QuotationId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public DateOnly OrderDate { get; private set; }
    public DateOnly? ExpectedDeliveryDate { get; private set; }
    public SalesOrderStatus Status { get; private set; } = SalesOrderStatus.Draft;
    public decimal SubtotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public Customer Customer { get; private set; } = default!;
    public Quotation? Quotation { get; private set; }
    public ICollection<SalesOrderItem> Items { get; private set; } = [];

    private SalesOrder() { }

    public static SalesOrder Create(
        Guid companyId,
        Guid branchId,
        Guid customerId,
        string orderNumber,
        DateOnly orderDate,
        DateOnly? expectedDeliveryDate,
        Guid createdByUserId,
        Guid? quotationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);

        return new SalesOrder
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            CustomerId = customerId,
            QuotationId = quotationId,
            OrderNumber = orderNumber.Trim(),
            OrderDate = orderDate,
            ExpectedDeliveryDate = expectedDeliveryDate,
            Status = SalesOrderStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void AddItem(Guid productId, decimal quantity, decimal unitPrice, decimal discountRate = 0)
    {
        if (Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Item pesanan hanya dapat ditambahkan pada status Draft.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Kuantitas pesanan harus lebih besar dari 0.");
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Harga per unit tidak boleh negatif.");

        var gross = quantity * unitPrice;
        var discount = gross * (discountRate / 100m);
        var lineTotal = Math.Round(gross - discount, 2);

        Items.Add(new SalesOrderItem
        {
            Id = Guid.NewGuid(),
            SalesOrderId = Id,
            ProductId = productId,
            Quantity = quantity,
            ShippedQuantity = 0,
            UnitPrice = unitPrice,
            DiscountRate = discountRate,
            LineTotal = lineTotal
        });

        RecalculateTotals();
    }

    public void RecalculateTotals(decimal taxRate = 0.11m)
    {
        SubtotalAmount = Items.Sum(i => i.LineTotal);
        TaxAmount = Math.Round(SubtotalAmount * taxRate, 2);
        TotalAmount = SubtotalAmount + TaxAmount;
    }

    public void Confirm(Guid approvedByUserId)
    {
        if (Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Hanya Sales Order Draft yang dapat dikonfirmasi.");
        if (Items.Count == 0)
            throw new InvalidOperationException("Sales Order harus memuat minimal satu item produk.");

        Status = SalesOrderStatus.Confirmed;
        ApprovedByUserId = approvedByUserId;
        UpdatedByUserId = approvedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordShipment(Guid soItemId, decimal shippedQty)
    {
        var item = Items.FirstOrDefault(i => i.Id == soItemId);
        if (item == null) throw new InvalidOperationException("Item SO tidak ditemukan.");

        item.ShippedQuantity += shippedQty;

        if (Items.All(i => i.ShippedQuantity >= i.Quantity))
        {
            Status = SalesOrderStatus.FullyShipped;
        }
        else if (Items.Any(i => i.ShippedQuantity > 0))
        {
            Status = SalesOrderStatus.PartiallyShipped;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkInvoiced()
    {
        Status = SalesOrderStatus.Invoiced;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class SalesOrderItem
{
    public Guid Id { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal LineTotal { get; set; }

    public SalesOrder SalesOrder { get; set; } = default!;
    public Product Product { get; set; } = default!;
}
