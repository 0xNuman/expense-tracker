namespace ExpenseTracker.Domain;

/// <summary>Strongly-typed identifier for an <see cref="Account"/>.</summary>
public readonly record struct AccountId(Guid Value) : IStrongId
{
    public static AccountId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}