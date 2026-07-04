using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExpenseTracker.Api.Persistence;

/// <summary>
/// Hosted service that applies EF Core migrations on startup. Skipped when
/// <c>Persistence:ApplyMigrationsOnStartup</c> is false. Uses an advisory
/// session lock so concurrent replicas do not double-apply on a slow DB.
/// </summary>
public sealed class MigrationsHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MigrationsHostedService> _logger;
    private readonly bool _enabled;

    public MigrationsHostedService(IServiceProvider services, ILogger<MigrationsHostedService> logger, IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _enabled = configuration.GetValue<bool>("Persistence:ApplyMigrationsOnStartup", defaultValue: true);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Migrations: ApplyMigrationsOnStartup=false; skipping.");
            return;
        }

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseTrackerDbContext>();

        try
        {
            await ApplyWithAdvisoryLockAsync(db, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var env = _services.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment())
            {
                _logger.LogWarning(ex, "Failed to apply EF Core migrations on startup (Development — continuing without DB).");
            }
            else
            {
                _logger.LogCritical(ex, "Failed to apply EF Core migrations on startup.");
                throw;
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task ApplyWithAdvisoryLockAsync(ExpenseTrackerDbContext db, CancellationToken ct)
    {
        var (key, success) = await TryAcquireLockAsync(db, ct);
        if (!success)
        {
            _logger.LogInformation("Migrations: another instance is applying; waiting for completion.");
            await WaitForMigrationsAsync(db, TimeSpan.FromSeconds(60), ct);
            return;
        }

        try
        {
            _logger.LogInformation("Migrations: applying pending migrations...");
            await db.Database.MigrateAsync(ct);
            _logger.LogInformation("Migrations: applied.");
        }
        finally
        {
            await ReleaseLockAsync(db, key, ct);
        }
    }

    /// <summary>Acquires a Postgres advisory lock keyed by a constant. Returns the lock key.</summary>
    private static async Task<(long Key, bool Acquired)> TryAcquireLockAsync(ExpenseTrackerDbContext db, CancellationToken ct)
    {
        const long key = 70_401L; // 'ET' → arbitrary fixed key shared by replicas
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT pg_try_advisory_lock(:key)";
        cmd.Parameters.AddWithValue("key", key);
        var result = await cmd.ExecuteScalarAsync(ct);
        return (key, result is true);
    }

    private static async Task ReleaseLockAsync(ExpenseTrackerDbContext db, long key, CancellationToken ct)
    {
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        if (conn.State == System.Data.ConnectionState.Closed) await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT pg_advisory_unlock(:key)";
        cmd.Parameters.AddWithValue("key", key);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task WaitForMigrationsAsync(ExpenseTrackerDbContext db, TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await db.Database.GetPendingMigrationsAsync(ct) is { } pending && !pending.Any())
                return;
            await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
        }
    }
}