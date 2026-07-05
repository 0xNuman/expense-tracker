using ExpenseTracker.Api.Features.Accounts;
using ExpenseTracker.Api.Features.Auth;
using ExpenseTracker.Api.Features.Transactions;
using ExpenseTracker.Api.Hal;
using ExpenseTracker.Api.Health;
using ExpenseTracker.Api.Persistence;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api;

/// <summary>Application entry point — wires the minimal API host.</summary>
public static class Program
{
    /// <summary>Main entry point.</summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddEndpointsApiExplorer()
            .AddOpenApi();

        builder.Services.AddExpenseTrackerPersistence(builder.Configuration);
        builder.Services.AddExpenseTrackerAuth(builder.Configuration);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi("/api/openapi.json");
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/", () => Results.Redirect("/api"))
           .ExcludeFromDescription();

        app.MapGet("/api", () => Results.Extensions.Hal(Endpoints.Root.GetRoot()))
           .WithName("Root")
           .WithSummary("HAL root — discover the API by following links.")
           .Produces<HalDocument>(statusCode: 200, contentType: HalDocument.MediaType);

        app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }))
           .ExcludeFromDescription()
           .WithTags("Health");

        app.MapGet("/health/ready", async (ExpenseTrackerDbContext db, CancellationToken ct) =>
        {
            var ready = await HealthChecker.IsReadyAsync(db, ct);
            return ready
                ? Results.Ok(new { status = "Healthy" })
                : Results.Json(new { status = "Degraded" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        })
           .ExcludeFromDescription()
           .WithTags("Health");

        app.MapAuth();
        app.MapAccounts();
        app.MapTransactions();

        app.Run();
    }
}