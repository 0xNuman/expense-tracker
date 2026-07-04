namespace ExpenseTracker.Domain;

/// <summary>Event raised when a refresh token is rotated (replaced by its successor).</summary>
public sealed record RefreshTokenRotated(RefreshTokenId NewTokenId, RefreshTokenId PreviousTokenId, UserId UserId, Guid FamilyId, DateTimeOffset OccurredAtUtc) : IDomainEvent;