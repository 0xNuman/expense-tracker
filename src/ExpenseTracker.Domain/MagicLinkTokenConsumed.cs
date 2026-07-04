namespace ExpenseTracker.Domain;

/// <summary>Event raised when a magic link token is successfully consumed.</summary>
public sealed record MagicLinkTokenConsumed(MagicLinkTokenId TokenId, UserId UserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;