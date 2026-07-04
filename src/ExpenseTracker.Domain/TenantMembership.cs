namespace ExpenseTracker.Domain;

/// <summary>Membership linking a <see cref="User"/> to a <see cref="Tenant"/> with a role.</summary>
public sealed class TenantMembership
{
    public TenantMembershipId Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public UserId UserId { get; private set; }
    public TenantRole Role { get; private set; }
    public UserId InvitedByUserId { get; private set; }
    public DateTimeOffset JoinedAtUtc { get; private set; }

    private TenantMembership() { }

    internal TenantMembership(TenantMembershipId id, TenantId tenantId, UserId userId, TenantRole role, UserId invitedByUserId, DateTimeOffset joinedAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        Role = role;
        InvitedByUserId = invitedByUserId;
        JoinedAtUtc = joinedAtUtc;
    }

    /// <summary>Factory for the owner-of-new-tenant bootstrap path.</summary>
    public static TenantMembership OwnerFor(TenantId tenantId, UserId userId, UserId createdBy)
    {
        return new TenantMembership(TenantMembershipId.New(), tenantId, userId, TenantRole.Owner, createdBy, DateTimeOffset.UtcNow);
    }

    internal void ChangeRole(TenantRole newRole)
    {
        if (newRole == Role) return;
        Role = newRole;
    }

    public bool IsOwner => Role == TenantRole.Owner;
    public bool IsAdminOrHigher => Role >= TenantRole.Admin;
}