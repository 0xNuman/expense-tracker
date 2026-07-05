namespace ExpenseTracker.Domain;

/// <summary>Strongly-typed identifier for a <see cref="PasskeyCredential"/>.</summary>
public readonly record struct PasskeyCredentialId(Guid Value) : IStrongId
{
    public static PasskeyCredentialId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}