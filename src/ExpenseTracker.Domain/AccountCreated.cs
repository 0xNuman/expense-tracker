namespace ExpenseTracker.Domain;

/// <summary>Event raised when an account is created.</summary>
public sealed record AccountCreated(AccountId AccountId, TenantId TenantId, string Name, DateTimeOffset OccurredAtUtc) : IDomainEvent;