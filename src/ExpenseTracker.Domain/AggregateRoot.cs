namespace ExpenseTracker.Domain;

/// <summary>
/// Base class for aggregate roots. Holds domain events collected during a unit of work
/// and dispatches them after persistence (the host wires the dispatcher).
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _events = new();

    /// <summary>Events collected since the aggregate was loaded or last committed.</summary>
    public IReadOnlyCollection<IDomainEvent> Events => _events;

    /// <summary>True if any events have been collected.</summary>
    public bool HasEvents => _events.Count > 0;

    protected void Raise(IDomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _events.Add(@event);
    }

    /// <summary>Clears collected events — called by the host after dispatch.</summary>
    public void ClearEvents() => _events.Clear();
}