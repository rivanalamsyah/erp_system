namespace ERP.Domain.Entities.Audit;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty; // INSERT, UPDATE, DELETE, SOFT_DELETE
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public Guid UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

