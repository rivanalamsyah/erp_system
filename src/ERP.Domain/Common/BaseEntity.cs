namespace ERP.Domain.Common;

public abstract class BaseEntity<TId> : ISoftDeletable
{
    public TId Id { get; protected set; } = default!;
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }

    // Audit Fields
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public virtual void SoftDelete(Guid deletedByUserId)
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        DeletedByUserId = deletedByUserId;
    }

    public virtual void UndoSoftDelete()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedByUserId = null;
    }

    // Optimistic Concurrency Token (PostgreSQL xmin)
    public uint Version { get; set; }
}

