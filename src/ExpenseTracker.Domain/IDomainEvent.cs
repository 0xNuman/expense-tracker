namespace ExpenseTracker.Domain;

/// <summary>Marker interface for domain events dispatched after SaveChangesAsync.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}