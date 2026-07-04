namespace ExpenseTracker.Domain;

/// <summary>Event raised when a refresh token is revoked (explicit or family-reuse).</summary>
public sealed record RefreshTokenRevoked(RefreshTokenId TokenId, UserId UserId, Guid FamilyId, DateTimeOffset OccurredAtUtc) : IDomainEvent;