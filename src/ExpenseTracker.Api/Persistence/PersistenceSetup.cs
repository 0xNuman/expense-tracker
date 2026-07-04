using ExpenseTracker.Api.Persistence;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api;

/// <summary>Registers persistence (EF Core + Postgres + migrations) in the DI container.</summary>
public static class PersistenceSetup
{
    /// <summary>Wires the DbContext, ITenantContext, and the migration runner.</summary>
    public static IServiceCollection AddExpenseTrackerPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is missing.");

        services.AddDbContext<ExpenseTrackerDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ExpenseTracker.Infrastructure.Persistence.ExpenseTrackerDbContext).Assembly.FullName);
            });
            if (System.Diagnostics.Debugger.IsAttached)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        services.AddHostedService<MigrationsHostedService>();

        return services;
    }
}