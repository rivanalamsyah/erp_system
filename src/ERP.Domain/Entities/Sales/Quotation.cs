using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Sales;

public enum QuotationStatus
{
    Draft,
    Sent,
    Approved,
    Rejected,
    Converted,
    Expired
}

public class Quotation : BaseEntity<Guid>
{
    public Guid CustomerId { get; private set; }
    public string QuotationNumber { get; private set; } = string.Empty;
    public DateOnly QuotationDate { get; private set; }
    public DateOnly ExpirationDate { get; private set; }
    public QuotationStatus Status { get; private set; } = QuotationStatus.Draft;
    public decimal SubtotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public Customer Customer { get; private set; } = default!;
    public ICollection<QuotationItem> Items { get; private set; } = [];

    private Quotation() { }

    public static Quotation Create(
        Guid companyId,
        Guid branchId,
        Guid customerId,
        string quotationNumber,
        DateOnly quotationDate,
        DateOnly expirationDate,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quotationNumber);

        return new Quotation
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            CustomerId = customerId,
            QuotationNumber = quotationNumber.Trim(),
            QuotationDate = quotationDate,
            ExpirationDate = expirationDate,
            Status = QuotationStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void AddItem(Guid productId, decimal quantity, decimal unitPrice, decimal discountRate = 0)
    {
        if (Status != QuotationStatus.Draft)
            throw new InvalidOperationException("Item penawaran hanya dapat ditambahkan pada status Draft.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Kuantitas harus lebih besar dari 0.");
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Harga per unit tidak boleh negatif.");

        var gross = quantity * unitPrice;
        var discount = gross * (discountRate / 100m);
        var lineTotal = Math.Round(gross - discount, 2);

        Items.Add(new QuotationItem
        {
            Id = Guid.NewGuid(),
            QuotationId = Id,
            ProductId = productId,
            Quantity = quantity,
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

    public void Approve(Guid userId)
    {
        if (Status != QuotationStatus.Draft && Status != QuotationStatus.Sent)
            throw new InvalidOperationException("Hanya penawaran Draft atau Sent yang dapat disetujui.");

        Status = QuotationStatus.Approved;
        UpdatedByUserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkConverted(Guid userId)
    {
        Status = QuotationStatus.Converted;
        UpdatedByUserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class QuotationItem
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal LineTotal { get; set; }

    public Quotation Quotation { get; set; } = default!;
    public Product Product { get; set; } = default!;
}

