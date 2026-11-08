using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Procurement;

public enum VendorBillStatus
{
    Draft,
    Posted,
    PartiallyPaid,
    Paid,
    Cancelled
}

public class VendorBill : BaseEntity<Guid>
{
    public Guid SupplierId { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public string BillNumber { get; private set; } = string.Empty;
    public string VendorInvoiceNumber { get; private set; } = string.Empty;
    public DateOnly BillDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount => TotalAmount - PaidAmount;
    public VendorBillStatus Status { get; private set; } = VendorBillStatus.Draft;

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public Supplier Supplier { get; private set; } = default!;
    public PurchaseOrder PurchaseOrder { get; private set; } = default!;

    private VendorBill() { }

    public static VendorBill Create(
        Guid companyId,
        Guid branchId,
        Guid supplierId,
        Guid purchaseOrderId,
        string billNumber,
        string vendorInvoiceNumber,
        DateOnly billDate,
        DateOnly dueDate,
        decimal totalAmount,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(billNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(vendorInvoiceNumber);
        if (totalAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "Total tagihan harus lebih besar dari 0.");

        return new VendorBill
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            SupplierId = supplierId,
            PurchaseOrderId = purchaseOrderId,
            BillNumber = billNumber.Trim(),
            VendorInvoiceNumber = vendorInvoiceNumber.Trim(),
            BillDate = billDate,
            DueDate = dueDate,
            TotalAmount = totalAmount,
            PaidAmount = 0,
            Status = VendorBillStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Post(Guid updatedByUserId)
    {
        if (Status != VendorBillStatus.Draft)
            throw new InvalidOperationException("Hanya tagihan berstatus Draft yang dapat diposting.");

        Status = VendorBillStatus.Posted;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordPayment(decimal paymentAmount)
    {
        if (Status != VendorBillStatus.Posted && Status != VendorBillStatus.PartiallyPaid)
            throw new InvalidOperationException("Pembayaran hanya dapat dicatat untuk tagihan yang telah diposting.");
        if (paymentAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(paymentAmount), "Nominal pembayaran harus lebih dari 0.");

        PaidAmount += paymentAmount;

        if (PaidAmount >= TotalAmount)
        {
            Status = VendorBillStatus.Paid;
        }
        else
        {
            Status = VendorBillStatus.PartiallyPaid;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }
}

