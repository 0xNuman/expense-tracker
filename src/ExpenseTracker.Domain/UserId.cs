namespace ExpenseTracker.Domain;

/// <summary>Strongly-typed identifier for a <see cref="User"/>.</summary>
public readonly record struct UserId(Guid Value) : IStrongId
{
    public static UserId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}