using System.Globalization;

namespace ExpenseTracker.Domain;

/// <summary>
/// Money value object — an amount in a specific currency.
/// Amount is stored with up to 4 decimal places (matching the DB column <c>numeric(18,4)</c>).
/// Currency mismatches between operands are rejected.
/// </summary>
public readonly record struct Money
{
    /// <summary>The amount in the given currency.</summary>
    public decimal Amount { get; }

    /// <summary>The currency of the amount.</summary>
    public CurrencyCode Currency { get; }

    private Money(decimal amount, CurrencyCode currency)
    {
        Amount = Math.Round(amount, 4, MidpointRounding.ToEven);
        Currency = currency;
    }

    /// <summary>Constructs a <see cref="Money"/> with the given amount and currency.</summary>
    public static Money Of(decimal amount, CurrencyCode currency)
    {
        if (currency.Value is null) throw new ArgumentException("Currency must be specified.", nameof(currency));
        return new Money(amount, currency);
    }

    /// <summary>Constructs a zero value in the given currency.</summary>
    public static Money Zero(CurrencyCode currency) => new(0m, currency);

    /// <summary>Returns <c>true</c> if the amount is zero.</summary>
    public bool IsZero => Amount == 0m;

    /// <summary>Adds two money values in the same currency. Mismatched currencies throw.</summary>
    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>Subtracts two money values in the same currency. Mismatched currencies throw.</summary>
    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    /// <summary>Scales the amount by a scalar factor (e.g. for FX conversion to the same currency).</summary>
    public Money Scale(decimal factor) => new(Amount * factor, Currency);

    /// <summary>Converts this money to another currency using the supplied FX rate.</summary>
    public Money ConvertTo(CurrencyCode target, FXRate rate)
    {
        if (!string.Equals(rate.FromCurrency.Value, Currency.Value, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"FX rate is for {rate.FromCurrency}→{rate.ToCurrency}; cannot convert {Currency}.");
        }
        if (!string.Equals(rate.ToCurrency.Value, target.Value, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"FX rate target {rate.ToCurrency} does not match requested {target}.");
        }
        return new Money(Amount * rate.Rate, target);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency.Value, other.Currency.Value, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Cannot combine money in {Currency} with money in {other.Currency}.");
        }
    }

    /// <summary>Formats the money per the supplied locale; null uses the current culture.</summary>
    public string Format(CultureInfo? culture = null) => $"{Amount.ToString("N", culture ?? CultureInfo.CurrentCulture)} {Currency}";

    /// <inheritdoc/>
    public override string ToString() => $"{Amount:0.####} {Currency}";
}