namespace ExpenseTracker.Api.Health;

/// <summary>Lightweight readiness probe — wired to real dependencies as phases progress.</summary>
public static class HealthChecker
{
    /// <summary>Walking-skeleton readiness: process is alive.</summary>
    public static Task<bool> IsReadyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(true);
    }
}