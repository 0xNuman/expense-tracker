namespace ExpenseTracker.Domain;

public enum RecurringRuleKind
{
    Income,
    Expense,
    Transfer
}

public enum RecurringCadence
{
    Daily,
    Weekly,
    Monthly,
    LastDayOfMonth,
    Yearly,
    CustomCron
}

public readonly record struct RecurringRuleId(Guid Value) : IStrongId
{
    public static RecurringRuleId New() => new(Guid.NewGuid());
    public static RecurringRuleId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

public sealed class RecurringRule : AggregateRoot
{
    public RecurringRuleId Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; }
    public bool Enabled { get; private set; }
    public RecurringRuleKind RuleKind { get; private set; }
    
    // Scheduling
    public RecurringCadence Cadence { get; private set; }
    public int Interval { get; private set; }
    public byte[]? DaysOfWeek { get; private set; }
    public int? DayOfMonth { get; private set; }
    public int? MonthOfYear { get; private set; }
    public DateOnly StartDateUtc { get; private set; }
    public DateOnly? EndDateUtc { get; private set; }
    public DateOnly NextRunUtc { get; private set; }

    // Txn Template
    public AccountId AccountId { get; private set; }
    public AccountId? CounterpartAccountId { get; private set; }
    public CategoryId? CategoryId { get; private set; }
    public Money AmountAccountCurrency { get; private set; }
    public Guid? FxSnapshotId { get; private set; }
    public string? MemoPattern { get; private set; }
    public string[] Tags { get; private set; } = [];

    // Behaviour
    public bool AutoPost { get; private set; }
    public int GraceDays { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }
    public TransactionId? LastRunTxnId { get; private set; }
    public bool Completed { get; private set; }

    private RecurringRule() { Name = null!; }

    public static RecurringRule Create(
        TenantId tenantId,
        string name,
        RecurringRuleKind kind,
        RecurringCadence cadence,
        int interval,
        DateOnly startDateUtc,
        AccountId accountId,
        Money amount,
        bool autoPost = true)
    {
        var rule = new RecurringRule
        {
            Id = RecurringRuleId.New(),
            TenantId = tenantId,
            Name = name,
            Enabled = true,
            RuleKind = kind,
            Cadence = cadence,
            Interval = interval,
            StartDateUtc = startDateUtc,
            NextRunUtc = startDateUtc,
            AccountId = accountId,
            AmountAccountCurrency = amount,
            AutoPost = autoPost
        };
        return rule;
    }

    public void Pause()
    {
        Enabled = false;
    }

    public void Resume(DateOnly nowUtc)
    {
        Enabled = true;
        NextRunUtc = nowUtc; // Or recompute
    }

    public void AdvanceNextRun()
    {
        // Simple advance logic for Phase 1
        switch (Cadence)
        {
            case RecurringCadence.Daily:
                NextRunUtc = NextRunUtc.AddDays(Interval);
                break;
            case RecurringCadence.Weekly:
                NextRunUtc = NextRunUtc.AddDays(7 * Interval);
                break;
            case RecurringCadence.Monthly:
                NextRunUtc = NextRunUtc.AddMonths(Interval);
                break;
            case RecurringCadence.Yearly:
                NextRunUtc = NextRunUtc.AddYears(Interval);
                break;
        }

        if (EndDateUtc.HasValue && NextRunUtc > EndDateUtc.Value)
        {
            Completed = true;
            Enabled = false;
        }
    }

    public void SetDayOfMonth(int day)
    {
        if (day < 1 || day > 31) throw new ArgumentOutOfRangeException(nameof(day));
        DayOfMonth = day;
    }

    public void SetCategory(CategoryId categoryId) => CategoryId = categoryId;
    public void SetMemo(string memo) => MemoPattern = memo;
    public void SetCounterpartAccount(AccountId counterpart) => CounterpartAccountId = counterpart;

    public void RecordRun(TransactionId txnId, DateTimeOffset runAt)
    {
        LastRunTxnId = txnId;
        LastRunAt = runAt;
        AdvanceNextRun();
    }
}
