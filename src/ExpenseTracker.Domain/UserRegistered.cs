namespace ExpenseTracker.Domain;

/// <summary>Event raised when a new user self-registers via magic link.</summary>
public sealed record UserRegistered(UserId UserId, string Email, DateTimeOffset OccurredAtUtc) : IDomainEvent;