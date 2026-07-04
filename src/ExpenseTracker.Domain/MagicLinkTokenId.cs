namespace ExpenseTracker.Domain;

/// <summary>Strongly-typed identifier for a <see cref="MagicLinkToken"/>.</summary>
public readonly record struct MagicLinkTokenId(Guid Value) : IStrongId
{
    public static MagicLinkTokenId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}