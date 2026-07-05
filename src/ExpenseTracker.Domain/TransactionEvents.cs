namespace ExpenseTracker.Domain;

/// <summary>Event raised when a transaction is created.</summary>
public sealed record TransactionCreated(TransactionId TransactionId, TenantId TenantId, AccountId AccountId, TransactionType Type, decimal Amount, CurrencyCode Currency, DateOnly OccurredOn, DateTimeOffset OccurredAtUtc) : IDomainEvent;

/// <summary>Event raised when a transaction is voided.</summary>
public sealed record TransactionVoided(TransactionId TransactionId, TenantId TenantId, DateTimeOffset OccurredAtUtc) : IDomainEvent;