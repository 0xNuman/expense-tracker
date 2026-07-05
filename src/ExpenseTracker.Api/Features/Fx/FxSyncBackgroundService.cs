using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Api.Features.Fx;

public class FxSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FxSyncBackgroundService> _logger;

    public FxSyncBackgroundService(IServiceProvider serviceProvider, ILogger<FxSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait briefly to allow EF migrations (MigrationsHostedService) to complete on startup
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("FxSyncBackgroundService running at: {time}", DateTimeOffset.Now);
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var fxProvider = scope.ServiceProvider.GetRequiredService<IExchangeRateProvider>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ExpenseTrackerDbContext>();

                // We can pick a default base currency, e.g., USD
                var baseCurrency = CurrencyCode.From("USD");
                var quotes = await fxProvider.GetRatesAsync(baseCurrency, null, stoppingToken);

                if (quotes.Any())
                {
                    // Basic caching approach: remove old from USD rates, insert new ones
                    var oldRates = dbContext.CachedRates.Where(r => r.FromCurrency == baseCurrency);
                    dbContext.CachedRates.RemoveRange(oldRates);

                    var cachedRates = quotes.Select(q => CachedRate.Create(
                        q.FromCurrency,
                        q.ToCurrency,
                        q.Rate,
                        q.FetchedAtUtc,
                        q.Source));

                    await dbContext.CachedRates.AddRangeAsync(cachedRates, stoppingToken);
                    await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Successfully refreshed {Count} FX rates from {Source}.", quotes.Count, quotes.First().Source);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred fetching FX rates.");
                // Retry sooner if it fails
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            // Sleep for 6 hours
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
