namespace ExpenseTracker.Domain;

/// <summary>Strongly-typed identifier for a <see cref="TenantMembership"/>.</summary>
public readonly record struct TenantMembershipId(Guid Value) : IStrongId
{
    public static TenantMembershipId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}