using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Procurement;

public enum PurchaseRequestStatus
{
    Draft,
    PendingApproval,
    Approved,
    Rejected,
    Closed
}

public class PurchaseRequest : BaseEntity<Guid>
{
    public string RequestNumber { get; private set; } = string.Empty;
    public DateOnly RequestDate { get; private set; }
    public DateOnly RequiredDate { get; private set; }
    public PurchaseRequestStatus Status { get; private set; } = PurchaseRequestStatus.Draft;
    public string? Notes { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public ICollection<PurchaseRequestItem> Items { get; private set; } = [];

    private PurchaseRequest() { }

    public static PurchaseRequest Create(
        Guid companyId,
        Guid branchId,
        string requestNumber,
        DateOnly requestDate,
        DateOnly requiredDate,
        string? notes,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestNumber);

        return new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            RequestNumber = requestNumber.Trim(),
            RequestDate = requestDate,
            RequiredDate = requiredDate,
            Status = PurchaseRequestStatus.Draft,
            Notes = notes?.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void AddItem(Guid productId, decimal quantity, decimal estimatedUnitPrice)
    {
        if (Status != PurchaseRequestStatus.Draft)
            throw new InvalidOperationException("Item PR hanya dapat ditambahkan pada status Draft.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Kuantitas PR harus lebih besar dari 0.");

        Items.Add(new PurchaseRequestItem
        {
            Id = Guid.NewGuid(),
            RequestId = Id,
            ProductId = productId,
            Quantity = quantity,
            EstimatedUnitPrice = Math.Max(0, estimatedUnitPrice)
        });
    }

    public void SubmitForApproval(Guid userId)
    {
        if (Status != PurchaseRequestStatus.Draft)
            throw new InvalidOperationException("Hanya PR berstatus Draft yang dapat diajukan persetujuan.");
        if (Items.Count == 0)
            throw new InvalidOperationException("PR harus memuat minimal satu item produk.");

        Status = PurchaseRequestStatus.PendingApproval;
        UpdatedByUserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status != PurchaseRequestStatus.PendingApproval)
            throw new InvalidOperationException("Hanya PR berstatus PendingApproval yang dapat disetujui.");

        Status = PurchaseRequestStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        UpdatedByUserId = approvedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reject(Guid rejectedByUserId)
    {
        if (Status != PurchaseRequestStatus.PendingApproval)
            throw new InvalidOperationException("Hanya PR berstatus PendingApproval yang dapat ditolak.");

        Status = PurchaseRequestStatus.Rejected;
        UpdatedByUserId = rejectedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Close(Guid userId)
    {
        Status = PurchaseRequestStatus.Closed;
        UpdatedByUserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class PurchaseRequestItem
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal EstimatedUnitPrice { get; set; }

    public PurchaseRequest Request { get; set; } = default!;
    public Product Product { get; set; } = default!;
}

