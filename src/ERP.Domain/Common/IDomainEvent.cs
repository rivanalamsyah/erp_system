namespace ERP.Domain.Common;

public interface IDomainEvent
{
    Guid EventId => Guid.NewGuid();
    DateTime OccurredOnUtc => DateTime.UtcNow;
}

