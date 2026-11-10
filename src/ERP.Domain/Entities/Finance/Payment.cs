using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Finance;

public enum PaymentType
{
    CustomerReceipt,
    VendorPayment
}

public enum PaymentMethod
{
    BankTransfer,
    Cash,
    Giro,
    Cheque
}

public enum PaymentStatus
{
    Draft,
    Posted,
    Voided
}

public class Payment : BaseEntity<Guid>
{
    public Guid CashBankAccountId { get; private set; }
    public string PaymentNumber { get; private set; } = string.Empty;
    public DateOnly PaymentDate { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public Guid PartnerId { get; private set; } // CustomerId or SupplierId
    public PaymentMethod PaymentMethod { get; private set; }
    public decimal TotalAmount { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Draft;

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public CashBankAccount CashBankAccount { get; private set; } = default!;
    public ICollection<PaymentAllocation> Allocations { get; private set; } = [];

    private Payment() { }

    public static Payment Create(
        Guid companyId,
        Guid branchId,
        Guid cashBankAccountId,
        string paymentNumber,
        DateOnly paymentDate,
        PaymentType paymentType,
        Guid partnerId,
        PaymentMethod paymentMethod,
        decimal totalAmount,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentNumber);
        if (totalAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "Total pembayaran harus lebih besar dari 0.");

        return new Payment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            CashBankAccountId = cashBankAccountId,
            PaymentNumber = paymentNumber.Trim(),
            PaymentDate = paymentDate,
            PaymentType = paymentType,
            PartnerId = partnerId,
            PaymentMethod = paymentMethod,
            TotalAmount = totalAmount,
            Status = PaymentStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void AddAllocation(Guid invoiceId, decimal allocatedAmount)
    {
        if (Status != PaymentStatus.Draft)
            throw new InvalidOperationException("Alokasi pembayaran hanya dapat ditambahkan pada status Draft.");
        if (allocatedAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(allocatedAmount), "Nominal alokasi harus lebih dari 0.");

        Allocations.Add(new PaymentAllocation
        {
            Id = Guid.NewGuid(),
            PaymentId = Id,
            InvoiceId = invoiceId,
            AllocatedAmount = allocatedAmount
        });
    }

    public void Post(Guid userId)
    {
        if (Status != PaymentStatus.Draft)
            throw new InvalidOperationException("Hanya pembayaran berstatus Draft yang dapat diposting.");

        var totalAllocated = Allocations.Sum(a => a.AllocatedAmount);
        if (Allocations.Count > 0 && totalAllocated > TotalAmount)
        {
            throw new InvalidOperationException(
                $"Total alokasi (Rp {totalAllocated:N2}) tidak boleh melebihi nilai total pembayaran (Rp {TotalAmount:N2}).");
        }

        Status = PaymentStatus.Posted;
        UpdatedByUserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class PaymentAllocation
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid InvoiceId { get; set; } // SalesInvoiceId or VendorBillId
    public decimal AllocatedAmount { get; set; }

    public Payment Payment { get; set; } = default!;
}

