namespace ExpenseTracker.Domain;

/// <summary>Strongly-typed identifier for a <see cref="RefreshToken"/>.</summary>
public readonly record struct RefreshTokenId(Guid Value) : IStrongId
{
    public static RefreshTokenId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}