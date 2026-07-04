namespace ExpenseTracker.Domain;

/// <summary>Strongly-typed identifier for a <see cref="Tenant"/>.</summary>
public readonly record struct TenantId(Guid Value) : IStrongId
{
    public static TenantId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}