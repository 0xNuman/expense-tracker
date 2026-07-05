namespace ExpenseTracker.Domain;

public enum RecurringExecutionStatus
{
    Posted,
    Skipped,
    Error
}

public readonly record struct RecurringExecutionLogId(Guid Value) : IStrongId
{
    public static RecurringExecutionLogId New() => new(Guid.NewGuid());
    public static RecurringExecutionLogId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

public sealed class RecurringExecutionLog
{
    public RecurringExecutionLogId Id { get; private set; }
    public RecurringRuleId RuleId { get; private set; }
    public DateOnly ScheduledForUtc { get; private set; }
    public TransactionId? PostedTxnId { get; private set; }
    public RecurringExecutionStatus Status { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset ExecutedAtUtc { get; private set; }

    private RecurringExecutionLog() { }

    public static RecurringExecutionLog Create(
        RecurringRuleId ruleId,
        DateOnly scheduledForUtc,
        TransactionId? postedTxnId,
        RecurringExecutionStatus status,
        string? error = null)
    {
        return new RecurringExecutionLog
        {
            Id = RecurringExecutionLogId.New(),
            RuleId = ruleId,
            ScheduledForUtc = scheduledForUtc,
            PostedTxnId = postedTxnId,
            Status = status,
            Error = error,
            ExecutedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
