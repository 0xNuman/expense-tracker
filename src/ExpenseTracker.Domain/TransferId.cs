namespace ExpenseTracker.Domain;

/// <summary>Strongly-typed identifier for a <see cref="Transfer"/>.</summary>
public readonly record struct TransferId(Guid Value) : IStrongId
{
    public static TransferId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
