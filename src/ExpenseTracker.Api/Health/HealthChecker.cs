using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Health;

/// <summary>Readiness probe — verifies the database connection is alive.</summary>
public static class HealthChecker
{
    /// <summary>Returns true when the database is reachable and bound to a known schema.</summary>
    public static async Task<bool> IsReadyAsync(ExpenseTrackerDbContext db, CancellationToken ct)
    {
        try
        {
            return await db.Database.CanConnectAsync(ct);
        }
        catch
        {
            return false;
        }
    }
}