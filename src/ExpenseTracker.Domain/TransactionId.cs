namespace ExpenseTracker.Domain;

/// <summary>Strongly-typed identifier for a <see cref="Transaction"/>.</summary>
public readonly record struct TransactionId(Guid Value) : IStrongId
{
    public static TransactionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>Strongly-typed identifier for a category.</summary>
public readonly record struct CategoryId(Guid Value) : IStrongId
{
    public static CategoryId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}