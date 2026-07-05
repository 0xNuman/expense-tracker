using Microsoft.Extensions.Caching.Memory;

namespace ExpenseTracker.Api.Auth;

/// <summary>
/// Lightweight per-key rate limiter using IMemoryCache.
/// Suitable for single-instance deployments (MVP). For multi-instance,
/// swap with Redis-backed counters in Phase 2+.
/// </summary>
public sealed class AuthRateLimiter
{
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;

    public AuthRateLimiter(IMemoryCache cache, TimeProvider? timeProvider = null)
    {
        _cache = cache;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Returns true if the request is allowed; false if rate-limited.
    /// Tracks a sliding window of request timestamps per key.
    /// </summary>
    public bool TryConsume(string partitionKey, int maxRequests, TimeSpan window)
    {
        var now = _timeProvider.GetUtcNow();
        var key = $"rl:{partitionKey}";
        var entries = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = window;
            return new List<DateTimeOffset>();
        })!;

        lock (entries)
        {
            entries.RemoveAll(t => now - t >= window);
            if (entries.Count >= maxRequests)
                return false;
            entries.Add(now);
            return true;
        }
    }

    /// <summary>Returns seconds until the oldest request in the window expires (for Retry-After).</summary>
    public int GetRetryAfterSeconds(string partitionKey, TimeSpan window)
    {
        var key = $"rl:{partitionKey}";
        if (_cache.TryGetValue(key, out List<DateTimeOffset>? entries) && entries is { Count: > 0 })
        {
            var oldest = entries[0];
            var retryAfter = oldest + window - _timeProvider.GetUtcNow();
            return Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        }
        return 60;
    }
}