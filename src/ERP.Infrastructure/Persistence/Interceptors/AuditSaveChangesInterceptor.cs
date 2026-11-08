using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ERP.Application.Common.Interfaces;
using ERP.Domain.Common;
using ERP.Domain.Entities.Audit;

namespace ERP.Infrastructure.Persistence.Interceptors;

public class AuditSaveChangesInterceptor(
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var userId = currentUserService.UserId ?? Guid.Empty;
        var now = dateTimeProvider.UtcNow;
        var auditLogs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Abaikan tabel AuditLog itu sendiri untuk mencegah rekursi tak hingga
            if (entry.Entity is AuditLog) continue;

            // 1. Audit Fields & Soft Delete
            if (entry.Entity is ISoftDeletable softDeletable)
            {
                if (entry.State == EntityState.Added)
                {
                    softDeletable.IsDeleted = false;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // Konversi ke Soft Delete
                    entry.State = EntityState.Modified;
                    softDeletable.IsDeleted = true;
                    softDeletable.DeletedAtUtc = now;
                    softDeletable.DeletedByUserId = userId;
                }
            }

            // Update created/updated timestamps pada BaseEntity
            var entityType = entry.Entity.GetType();
            var createdAtProp = entityType.GetProperty(nameof(BaseEntity<Guid>.CreatedAtUtc));
            var createdByProp = entityType.GetProperty(nameof(BaseEntity<Guid>.CreatedByUserId));
            var updatedAtProp = entityType.GetProperty(nameof(BaseEntity<Guid>.UpdatedAtUtc));
            var updatedByProp = entityType.GetProperty(nameof(BaseEntity<Guid>.UpdatedByUserId));

            if (entry.State == EntityState.Added)
            {
                createdAtProp?.SetValue(entry.Entity, now);
                createdByProp?.SetValue(entry.Entity, userId);
            }
            else if (entry.State == EntityState.Modified)
            {
                updatedAtProp?.SetValue(entry.Entity, now);
                updatedByProp?.SetValue(entry.Entity, userId);
            }

            // 2. Audit Trail JSON Capture
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                var idProp = entityType.GetProperty("Id");
                var entityId = idProp != null && idProp.GetValue(entry.Entity) is Guid g ? g : Guid.Empty;
                var companyIdProp = entityType.GetProperty("CompanyId");
                var companyId = companyIdProp != null && companyIdProp.GetValue(entry.Entity) is Guid cid ? cid : currentUserService.CompanyId;

                string action = entry.State switch
                {
                    EntityState.Added => "INSERT",
                    EntityState.Deleted => "DELETE",
                    EntityState.Modified when entry.Entity is ISoftDeletable { IsDeleted: true } => "SOFT_DELETE",
                    _ => "UPDATE"
                };

                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsPrimaryKey()) continue;

                    if (entry.State == EntityState.Added)
                    {
                        newValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        oldValues[prop.Metadata.Name] = prop.OriginalValue;
                    }
                    else if (entry.State == EntityState.Modified && prop.IsModified)
                    {
                        oldValues[prop.Metadata.Name] = prop.OriginalValue;
                        newValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }

                auditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    EntityName = entityType.Name,
                    EntityId = entityId,
                    Action = action,
                    OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
                    UserId = userId,
                    CreatedAtUtc = now
                });
            }
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }
}

