namespace ExpenseTracker.Domain;

public enum FXSnapshotMethod
{
    DailyFix = 0,
    SpotAtTime = 1,
    UserEntered = 2
}

public sealed class FXSnapshot : AggregateRoot
{
    public Guid SnapshotId { get; private set; }
    public CurrencyCode FromCurrency { get; private set; }
    public CurrencyCode ToCurrency { get; private set; }
    public decimal Rate { get; private set; }
    public DateTimeOffset FetchedAtUtc { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public FXSnapshotMethod Method { get; private set; }

    private FXSnapshot() { }

    public static FXSnapshot Create(CurrencyCode from, CurrencyCode to, decimal rate, DateTimeOffset fetchedAtUtc, string source, FXSnapshotMethod method)
    {
        return new FXSnapshot
        {
            SnapshotId = Guid.NewGuid(),
            FromCurrency = from,
            ToCurrency = to,
            Rate = rate,
            FetchedAtUtc = fetchedAtUtc,
            Source = source,
            Method = method
        };
    }
}
