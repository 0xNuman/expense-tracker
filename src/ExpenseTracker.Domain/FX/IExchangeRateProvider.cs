namespace ExpenseTracker.Domain;

public record RateQuote(
    CurrencyCode FromCurrency,
    CurrencyCode ToCurrency,
    decimal Rate,
    DateTimeOffset FetchedAtUtc,
    string Source
);

public interface IExchangeRateProvider
{
    Task<decimal?> GetRateAsync(CurrencyCode from, CurrencyCode to, DateTimeOffset? asOfUtc, CancellationToken ct = default);
    Task<IReadOnlyCollection<RateQuote>> GetRatesAsync(CurrencyCode baseCurrency, DateTimeOffset? asOfUtc, CancellationToken ct = default);
}
