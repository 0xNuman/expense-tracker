namespace ExpenseTracker.Domain;

/// <summary>Event raised when a placeholder user is created via tenant invitation.</summary>
public sealed record UserInvited(UserId UserId, string Email, DateTimeOffset OccurredAtUtc) : IDomainEvent;