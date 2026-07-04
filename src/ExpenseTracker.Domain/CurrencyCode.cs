using System.Globalization;

namespace ExpenseTracker.Domain;

/// <summary>
/// ISO 4217 currency code value object — three-letter uppercase, validated
/// against the .NET <see cref="RegionInfo"/> currency list at construction.
/// </summary>
public readonly record struct CurrencyCode
{
    /// <summary>The three-letter ISO 4217 code (e.g. USD, EUR, INR).</summary>
    public string Value { get; }

    /// <summary>Number of minor (subdivision) units. USD=2, JPY=0, etc.</summary>
    public int MinorUnits { get; }

    /// <summary>Symbol from the current culture, best-effort. May be empty.</summary>
    public string Symbol { get; }

    private CurrencyCode(string value, int minorUnits, string symbol)
    {
        Value = value;
        MinorUnits = minorUnits;
        Symbol = symbol;
    }

    /// <summary>Constructs a <see cref="CurrencyCode"/> from a 3-letter string.</summary>
    public static CurrencyCode From(string code)
    {
        if (code is null) throw new ArgumentNullException(nameof(code));
        var normalised = code.Trim().ToUpperInvariant();
        if (normalised.Length != 3 || !normalised.All(char.IsLetter))
        {
            throw new ArgumentException($"'{code}' is not a valid ISO 4217 currency code.", nameof(code));
        }

        var region = CurrencyRegistry.TryFind(normalised);
        if (region is null)
        {
            throw new ArgumentException($"Unknown ISO 4217 currency code '{normalised}'.", nameof(code));
        }

        var symbol = TryGetSymbol(normalised);
        return new CurrencyCode(normalised, region.Value.MinorUnits, symbol);
    }

    /// <summary>Returns the currency symbol when the runtime culture exposes one; otherwise the code itself.</summary>
    private static string TryGetSymbol(string code)
    {
        try
        {
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                if (culture.IsNeutralCulture) continue;
                var region = new RegionInfo(culture.LCID);
                if (string.Equals(region.ISOCurrencySymbol, code, StringComparison.Ordinal))
                {
                    return region.CurrencySymbol;
                }
            }
        }
        catch
        {
            // Fall through; symbol unknown.
        }

        return code;
    }

    /// <summary>Implicit conversion back to the underlying string for serialization.</summary>
    public static implicit operator string(CurrencyCode c) => c.Value;

    /// <inheritdoc/>
    public override string ToString() => Value;
}