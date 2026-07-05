using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Features.Fx;

public static class FxEndpoints
{
    public static void MapFxEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fx").RequireAuthorization();

        group.MapGet("/rates", async (
            [FromQuery] string? @base,
            [FromQuery] DateTimeOffset? asOf,
            [FromServices] IExchangeRateProvider fxProvider,
            ExpenseTrackerDbContext dbContext,
            CancellationToken ct) =>
        {
            var baseCurrency = CurrencyCode.From(@base ?? "USD");
            
            // Optionally try to fetch from DB first if it's within TTL, or just pass to provider
            // For MVP, we'll hit the provider for live rates or historical rates
            var rates = await fxProvider.GetRatesAsync(baseCurrency, asOf, ct);
            
            if (!rates.Any() && !asOf.HasValue)
            {
                // Fallback to latest CachedRate in DB
                var cached = await dbContext.CachedRates
                    .Where(r => r.FromCurrency == baseCurrency)
                    .ToListAsync(ct);
                    
                if (cached.Any())
                {
                    rates = cached.Select(c => new RateQuote(c.FromCurrency, c.ToCurrency, c.Rate, c.FetchedAtUtc, c.Source)).ToList();
                }
            }

            return Results.Ok(new
            {
                Base = baseCurrency.Value,
                AsOf = asOf ?? DateTimeOffset.UtcNow,
                Rates = rates.ToDictionary(r => r.ToCurrency.Value, r => new
                {
                    r.Rate,
                    r.FetchedAtUtc,
                    r.Source
                })
            });
        });

        group.MapPost("/snapshot", async (
            [FromBody] CreateSnapshotRequest req,
            ExpenseTrackerDbContext dbContext,
            CancellationToken ct) =>
        {
            // Note: In real app, check for Admin role
            var from = CurrencyCode.From(req.From);
            var to = CurrencyCode.From(req.To);

            var snapshot = FXSnapshot.Create(from, to, req.Rate, req.AsOfUtc ?? DateTimeOffset.UtcNow, "Manual", FXSnapshotMethod.UserEntered);
            
            dbContext.FXSnapshots.Add(snapshot);
            await dbContext.SaveChangesAsync(ct);

            return Results.Ok(new { snapshot.SnapshotId });
        });
    }

    public record CreateSnapshotRequest(string From, string To, decimal Rate, DateTimeOffset? AsOfUtc);
}
