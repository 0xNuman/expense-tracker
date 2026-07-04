namespace ExpenseTracker.Domain;

/// <summary>Event raised when a refresh token is issued.</summary>
public sealed record RefreshTokenIssued(RefreshTokenId TokenId, UserId UserId, Guid FamilyId, DateTimeOffset OccurredAtUtc) : IDomainEvent;