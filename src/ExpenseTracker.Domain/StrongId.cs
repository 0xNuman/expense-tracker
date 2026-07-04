namespace ExpenseTracker.Domain;

/// <summary>
/// Marker interface for strongly-typed identifiers backed by a <see cref="Guid"/>.
/// Enables uniform conversion conventions in EF Core and JSON serialization.
/// </summary>
public interface IStrongId
{
    /// <summary>The underlying GUID value.</summary>
    Guid Value { get; }

    /// <summary>String form used for logging and URLs.</summary>
    string ToString() => Value.ToString();
}