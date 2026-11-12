using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        var ipAddress = currentUserService.IpAddress ?? "127.0.0.1";
        var now = dateTimeProvider.UtcNow;
        var auditLogs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Abaikan tabel AuditLog itu sendiri untuk mencegah rekursi tak hingga
            if (entry.Entity is AuditLog) continue;

            // 1. Audit Fields & Soft Delete via strongly-typed interfaces (tanpa refleksi)
            if (entry.Entity is ISoftDeletable softDeletable)
            {
                if (entry.State == EntityState.Added)
                {
                    softDeletable.IsDeleted = false;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    softDeletable.IsDeleted = true;
                    softDeletable.DeletedAtUtc = now;
                    softDeletable.DeletedByUserId = userId;
                }
            }

            if (entry.Entity is BaseEntity<Guid> baseEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    baseEntity.CreatedAtUtc = now;
                    if (userId != Guid.Empty || baseEntity.CreatedByUserId == Guid.Empty)
                    {
                        baseEntity.CreatedByUserId = userId;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    baseEntity.UpdatedAtUtc = now;
                    baseEntity.UpdatedByUserId = userId;
                }
            }

            // 2. Audit Trail JSON Capture menggunakan EF Core Metadata (tanpa refleksi GetType().GetProperty)
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                var entityId = ResolveGuidProperty(entry, "Id") ?? Guid.Empty;
                var companyId = ResolveGuidProperty(entry, "CompanyId") ?? currentUserService.CompanyId;

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
                    EntityName = entry.Metadata.ClrType.Name,
                    EntityId = entityId,
                    Action = action,
                    OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
                    UserId = userId,
                    IpAddress = ipAddress,
                    CreatedAtUtc = now
                });
            }
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }

    private static Guid? ResolveGuidProperty(EntityEntry entry, string propertyName)
    {
        if (entry.Entity is BaseEntity<Guid> baseEntity)
        {
            if (propertyName == "Id") return baseEntity.Id;
            if (propertyName == "CompanyId") return baseEntity.CompanyId;
        }

        var propMeta = entry.Metadata.FindProperty(propertyName);
        if (propMeta != null && entry.Property(propertyName).CurrentValue is Guid guidVal)
        {
            return guidVal;
        }

        return null;
    }
}
