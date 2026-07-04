namespace ExpenseTracker.Domain;

/// <summary>Event raised when a user is invited to a tenant with a specific role.</summary>
public sealed record TenantMemberInvited(TenantId TenantId, UserId UserId, TenantRole Role, UserId InvitedByUserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;