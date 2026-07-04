namespace ExpenseTracker.Domain;

/// <summary>
/// FX (foreign exchange) rate value object — a multiplier from <see cref="FromCurrency"/>
/// to <see cref="ToCurrency"/> captured at a point in time. Rate is per-unit (e.g. USD→INR ≈ 83.20).
/// </summary>
public readonly record struct FXRate
{
    /// <summary>Source currency.</summary>
    public CurrencyCode FromCurrency { get; }

    /// <summary>Target currency.</summary>
    public CurrencyCode ToCurrency { get; }

    /// <summary>Multiplier: amount(From) * Rate = amount(To).</summary>
    public decimal Rate { get; }

    /// <summary>UTC timestamp when the rate was observed.</summary>
    public DateTimeOffset FetchedAtUtc { get; }

    /// <summary>Identifier of the provider/source (e.g. 'ECB', 'Manual', 'OpenExchangeRates').</summary>
    public string Source { get; }

    private FXRate(CurrencyCode from, CurrencyCode to, decimal rate, DateTimeOffset fetchedAtUtc, string source)
    {
        FromCurrency = from;
        ToCurrency = to;
        Rate = rate;
        FetchedAtUtc = fetchedAtUtc;
        Source = source;
    }

    /// <summary>Constructs an FX rate. <paramref name="rate"/> must be strictly positive.</summary>
    public static FXRate Of(CurrencyCode from, CurrencyCode to, decimal rate, DateTimeOffset fetchedAtUtc, string source)
    {
        if (from.Value is null) throw new ArgumentException("Source currency is required.", nameof(from));
        if (to.Value is null) throw new ArgumentException("Target currency is required.", nameof(to));
        if (rate <= 0m) throw new ArgumentOutOfRangeException(nameof(rate), "FX rate must be positive.");
        if (string.IsNullOrEmpty(source)) throw new ArgumentException("Source is required.", nameof(source));
        return new FXRate(from, to, rate, fetchedAtUtc, source);
    }

    /// <summary>Builds the inverse rate (To→From). Useful for triangulation.</summary>
    public FXRate Invert() => new(ToCurrency, FromCurrency, 1m / Rate, FetchedAtUtc, Source + "-inverse");

    /// <summary>Multiplies a from-currency amount by the rate to produce the to-currency amount.</summary>
    public decimal Convert(decimal amount) => Math.Round(amount * Rate, 4, MidpointRounding.ToEven);

    /// <inheritdoc/>
    public override string ToString() => $"{Rate:0.####} {FromCurrency}→{ToCurrency} ({Source})";
}