using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using ExpenseTracker.Api;

namespace ExpenseTracker.IntegrationTests;

public class ExpenseTrackerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("expensetracker_test")
        .WithUsername("et_test")
        .WithPassword("et_test")
        .Build();

    public string DbConnectionString => _dbContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ExpenseTrackerDbContext>));
            services.RemoveAll(typeof(ExpenseTrackerDbContext));

            services.AddDbContext<ExpenseTrackerDbContext>(options =>
                options.UseNpgsql(DbConnectionString));
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    new public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
