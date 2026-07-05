namespace ExpenseTracker.Domain;

/// <summary>
/// Cached FX rate stored in the database.
/// TTL is usually evaluated at query time or via a background cleanup.
/// </summary>
public sealed class CachedRate
{
    public Guid Id { get; private set; }
    public CurrencyCode FromCurrency { get; private set; }
    public CurrencyCode ToCurrency { get; private set; }
    public decimal Rate { get; private set; }
    public DateTimeOffset FetchedAtUtc { get; private set; }
    public string Source { get; private set; } = string.Empty;

    private CachedRate() { }

    public static CachedRate Create(CurrencyCode from, CurrencyCode to, decimal rate, DateTimeOffset fetchedAtUtc, string source)
    {
        return new CachedRate
        {
            Id = Guid.NewGuid(),
            FromCurrency = from,
            ToCurrency = to,
            Rate = rate,
            FetchedAtUtc = fetchedAtUtc,
            Source = source
        };
    }
}
