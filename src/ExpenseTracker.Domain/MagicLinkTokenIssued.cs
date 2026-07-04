namespace ExpenseTracker.Domain;

/// <summary>Event raised when a magic link token is issued for an email.</summary>
public sealed record MagicLinkTokenIssued(MagicLinkTokenId TokenId, string NormalizedEmail, DateTimeOffset ExpiresAtUtc, DateTimeOffset OccurredAtUtc) : IDomainEvent;