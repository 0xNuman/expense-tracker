namespace ExpenseTracker.Domain;

/// <summary>
/// A tenant workspace — an isolated boundary for accounts, categories,
/// transactions, transfers, recurring rules and budgets. A user may belong
/// to many tenants via <see cref="TenantMembership"/> rows.
/// </summary>
public sealed class Tenant : AggregateRoot
{
    public TenantId Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Unique display name for the tenant (per-owner unique is enforced by service).</summary>
    public string DisplayName { get; private set; } = string.Empty;

    public UserId CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private readonly List<TenantMembership> _memberships = new();
    public IReadOnlyCollection<TenantMembership> Memberships => _memberships;

    private Tenant() { }

    /// <summary>
    /// Creates a new tenant, adds the creating user as Owner, and raises a domain event.
    /// </summary>
    public static Tenant Create(string name, UserId createdByUserId)
    {
        ValidateName(name);

        var tenant = new Tenant
        {
            Id = TenantId.New(),
            Name = name.Trim(),
            DisplayName = name.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        tenant._memberships.Add(TenantMembership.OwnerFor(tenant.Id, createdByUserId, createdByUserId));
        tenant.Raise(new TenantCreated(tenant.Id, tenant.Name, createdByUserId, tenant.CreatedAtUtc));
        return tenant;
    }

    /// <summary>Renames the tenant. Empty/duplicate-against-self rejected by validation.</summary>
    public void Rename(string name)
    {
        ValidateName(name);
        Name = name.Trim();
        DisplayName = name.Trim();
    }

    /// <summary>Invites a user identity to this tenant as <paramref name="role"/>.</summary>
    public TenantMembership Invite(UserId userId, TenantRole role, UserId invitedBy)
    {
        EnsureNotAlreadyMember(userId);
        EnsureNoSecondOwner(role);
        var membership = new TenantMembership(TenantMembershipId.New(), Id, userId, role, invitedBy, DateTimeOffset.UtcNow);
        _memberships.Add(membership);
        Raise(new TenantMemberInvited(Id, userId, role, invitedBy, DateTimeOffset.UtcNow));
        return membership;
    }

    /// <summary>Adds a membership directly (used during signup bootstrap or accept-invitation flow).</summary>
    public void AddMembership(TenantMembership membership)
    {
        EnsureNotAlreadyMember(membership.UserId);
        EnsureNoSecondOwner(membership.Role);
        _memberships.Add(membership);
    }

    /// <summary>Changes a member's role. Owner transfer requires specifying the new owner.</summary>
    public void ChangeMemberRole(TenantMembership membership, TenantRole newRole, UserId? newOwnerUserId = null)
    {
        if (membership.TenantId != Id) throw new InvalidOperationException("Membership not in this tenant.");
        EnsureNoSecondOwner(newRole);
        var wasOwner = membership.Role == TenantRole.Owner;
        membership.ChangeRole(newRole);

        if (wasOwner && newRole != TenantRole.Owner)
        {
            // Demoting the owner requires a successor; otherwise invariant breaks (no Owner).
            if (newOwnerUserId is null)
            {
                throw new InvalidOperationException("Cannot demote the owner without specifying a successor owner.");
            }
            var successor = _memberships.FirstOrDefault(m => m.UserId == newOwnerUserId.Value);
            if (successor is null)
                throw new InvalidOperationException("Successor owner is not a member of this tenant.");
            successor.ChangeRole(TenantRole.Owner);
        }
    }

    /// <summary>Removes a member. Owner removal requires transfer first; otherwise invariant breaks.</summary>
    public void RemoveMember(TenantMembership membership)
    {
        if (membership.TenantId != Id) throw new InvalidOperationException("Membership not in this tenant.");
        if (membership.Role == TenantRole.Owner && _memberships.Count == 1)
            throw new InvalidOperationException("Cannot remove the only Owner of a tenant.");
        _memberships.Remove(membership);
    }

    public bool HasOwner(out TenantMembership? owner)
    {
        owner = _memberships.FirstOrDefault(m => m.Role == TenantRole.Owner);
        return owner is not null;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("Tenant name must be ≤ 100 chars.", nameof(name));
    }

    private void EnsureNotAlreadyMember(UserId userId)
    {
        if (_memberships.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException($"User {userId} is already a member of tenant {Id}.");
        }
    }

    private void EnsureNoSecondOwner(TenantRole role)
    {
        if (role == TenantRole.Owner && _memberships.Any(m => m.Role == TenantRole.Owner))
        {
            throw new InvalidOperationException("Tenant already has an Owner; cannot add a second.");
        }
    }
}