using System.Collections.ObjectModel;

namespace ExpenseTracker.Domain;

/// <summary>
/// Static registry of ISO 4217 currency codes known to the runtime.
/// Backed by <see cref="System.Globalization.RegionInfo"/> enumeration at first use.
/// </summary>
internal static class CurrencyRegistry
{
    private static readonly ReadOnlyDictionary<string, int> Registry = BuildRegistry();

    /// <summary>Tries to find the currency with minor-unit precision.</summary>
    public static (string Code, int MinorUnits)? TryFind(string code)
    {
        return Registry.TryGetValue(code, out var minor) ? (code, minor) : null;
    }

    private static ReadOnlyDictionary<string, int> BuildRegistry()
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var culture in System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.SpecificCultures))
        {
            if (culture.IsNeutralCulture) continue;
            try
            {
                var region = new System.Globalization.RegionInfo(culture.LCID);
                if (region is null) continue;
                var code = region.ISOCurrencySymbol;
                if (!string.IsNullOrEmpty(code))
                {
                    map[code] = culture.NumberFormat.CurrencyDecimalDigits;
                }
            }
            catch
            {
                // Some LCIDs are not mappable to RegionInfo; skip.
            }
        }

        // Defensive defaults for common codes not always returned by GetCultures on every platform.
        foreach (var fallback in new[] { ("USD", 2), ("EUR", 2), ("GBP", 2), ("JPY", 0), ("INR", 2), ("AUD", 2), ("CAD", 2), ("CHF", 2), ("CNY", 2), ("SGD", 2), ("AED", 2), ("ZAR", 2) })
        {
            map.TryAdd(fallback.Item1, fallback.Item2);
        }

        return new ReadOnlyDictionary<string, int>(map);
    }
}