using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Sales;

public enum SalesInvoiceStatus
{
    Draft,
    Posted,
    PartiallyPaid,
    Paid,
    Cancelled
}

public class SalesInvoice : BaseEntity<Guid>
{
    public Guid CustomerId { get; private set; }
    public Guid SalesOrderId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public string? TaxInvoiceNumber { get; private set; } // Faktur Pajak
    public DateOnly InvoiceDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount => TotalAmount - PaidAmount;
    public SalesInvoiceStatus Status { get; private set; } = SalesInvoiceStatus.Draft;

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public Customer Customer { get; private set; } = default!;
    public SalesOrder SalesOrder { get; private set; } = default!;

    private SalesInvoice() { }

    public static SalesInvoice Create(
        Guid companyId,
        Guid branchId,
        Guid customerId,
        Guid salesOrderId,
        string invoiceNumber,
        string? taxInvoiceNumber,
        DateOnly invoiceDate,
        DateOnly dueDate,
        decimal subtotalAmount,
        decimal taxAmount,
        decimal totalAmount,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        if (totalAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "Total faktur harus lebih besar dari 0.");

        return new SalesInvoice
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            CustomerId = customerId,
            SalesOrderId = salesOrderId,
            InvoiceNumber = invoiceNumber.Trim(),
            TaxInvoiceNumber = taxInvoiceNumber?.Trim(),
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            SubtotalAmount = subtotalAmount,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            PaidAmount = 0,
            Status = SalesInvoiceStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Post(Guid updatedByUserId)
    {
        if (Status != SalesInvoiceStatus.Draft)
            throw new InvalidOperationException("Hanya faktur berstatus Draft yang dapat diposting.");

        Status = SalesInvoiceStatus.Posted;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordPayment(decimal paymentAmount)
    {
        if (Status != SalesInvoiceStatus.Posted && Status != SalesInvoiceStatus.PartiallyPaid)
            throw new InvalidOperationException("Pembayaran hanya dapat dicatat untuk faktur yang telah diposting.");
        if (paymentAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(paymentAmount), "Nominal pembayaran piutang harus lebih dari 0.");

        PaidAmount += paymentAmount;

        if (PaidAmount >= TotalAmount)
        {
            Status = SalesInvoiceStatus.Paid;
        }
        else
        {
            Status = SalesInvoiceStatus.PartiallyPaid;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }
}
