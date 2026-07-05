using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ExpenseTracker.Domain;

namespace ExpenseTracker.Infrastructure.FX;

public class FrankfurterExchangeRateProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private const string SourceName = "Frankfurter";

    public FrankfurterExchangeRateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.frankfurter.app/");
    }

    public async Task<decimal?> GetRateAsync(CurrencyCode from, CurrencyCode to, DateTimeOffset? asOfUtc, CancellationToken ct = default)
    {
        if (from == to) return 1m;

        var date = asOfUtc.HasValue ? asOfUtc.Value.ToString("yyyy-MM-dd") : "latest";
        var url = $"{date}?from={from.Value}&to={to.Value}";
        
        try
        {
            var response = await _httpClient.GetFromJsonAsync<FrankfurterResponse>(url, ct);
            if (response != null && response.Rates.TryGetValue(to.Value, out var rate))
            {
                return rate;
            }
        }
        catch (HttpRequestException)
        {
            // Logging can be added here
        }

        return null;
    }

    public async Task<IReadOnlyCollection<RateQuote>> GetRatesAsync(CurrencyCode baseCurrency, DateTimeOffset? asOfUtc, CancellationToken ct = default)
    {
        var date = asOfUtc.HasValue ? asOfUtc.Value.ToString("yyyy-MM-dd") : "latest";
        var url = $"{date}?from={baseCurrency.Value}";
        var quotes = new List<RateQuote>();

        try
        {
            var response = await _httpClient.GetFromJsonAsync<FrankfurterResponse>(url, ct);
            if (response != null)
            {
                var fetchedAt = asOfUtc ?? DateTimeOffset.UtcNow;
                foreach (var (currency, rate) in response.Rates)
                {
                    quotes.Add(new RateQuote(baseCurrency, CurrencyCode.From(currency), rate, fetchedAt, SourceName));
                }
            }
        }
        catch (HttpRequestException)
        {
            // Logging can be added here
        }

        return quotes;
    }

    private class FrankfurterResponse
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("base")]
        public string Base { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
