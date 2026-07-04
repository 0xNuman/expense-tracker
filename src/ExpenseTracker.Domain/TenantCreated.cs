namespace ExpenseTracker.Domain;

/// <summary>Event raised when a new tenant workspace is created.</summary>
public sealed record TenantCreated(TenantId TenantId, string Name, UserId CreatedByUserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;