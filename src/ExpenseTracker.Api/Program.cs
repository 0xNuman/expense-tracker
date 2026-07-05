using ExpenseTracker.Api.Features.Accounts;
using ExpenseTracker.Api.Features.Auth;
using ExpenseTracker.Api.Features.Categories;
using ExpenseTracker.Api.Features.CsvIo;
using ExpenseTracker.Api.Features.Fx;
using ExpenseTracker.Api.Features.RecurringTransactions;
using ExpenseTracker.Api.Features.Reports;
using ExpenseTracker.Api.Features.Transactions;
using ExpenseTracker.Api.Features.Transfers;
using ExpenseTracker.Api.Hal;
using ExpenseTracker.Api.Health;
using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.FX;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.RateLimiting;
using ExpenseTracker.Api.Auth;

namespace ExpenseTracker.Api;

/// <summary>Application entry point — wires the minimal API host.</summary>
public class Program
{
    /// <summary>Main entry point.</summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. CORS Allowlist
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
                             ["http://localhost:5173"];
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("StrictCors", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // 2. Rate Limiting
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("ApiPolicy", limiterOptions =>
            {
                limiterOptions.PermitLimit = 100;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });
        });

        // 3. Problem Details
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        // 4. Memory Cache for Idempotency
        builder.Services.AddMemoryCache();

        builder.Services
            .AddEndpointsApiExplorer()
            .AddOpenApi();

        builder.Services.AddExpenseTrackerPersistence(builder.Configuration);
        builder.Services.AddExpenseTrackerAuth(builder.Configuration);

        // 5. FX Services
        builder.Services.AddHttpClient<IExchangeRateProvider, FrankfurterExchangeRateProvider>();
        builder.Services.AddHostedService<FxSyncBackgroundService>();

        var app = builder.Build();

        app.UseExceptionHandler(); // Adds ProblemDetails

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi("/api/openapi.json");
        }

        app.UseHttpsRedirection();
        app.UseCors("StrictCors");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.UseMiddleware<IdempotencyMiddleware>();

        app.MapGet("/", () => Results.Redirect("/api"))
            .ExcludeFromDescription();

        // Group API endpoints to apply Rate Limiting globally across API routes
        var apiGroup = app.MapGroup("").RequireRateLimiting("ApiPolicy");

        apiGroup.MapGet("/api", () => Results.Extensions.Hal(Endpoints.Root.GetRoot()))
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

        apiGroup.MapAuth();
        apiGroup.MapAccounts();
        apiGroup.MapTransactions();
        apiGroup.MapCategories();
        apiGroup.MapTransfers();
        apiGroup.MapReports();
        apiGroup.MapCsvIoEndpoints();
        apiGroup.MapFxEndpoints();
        apiGroup.MapRecurringTransactions();

        app.Run();
    }
}

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
            Detail = exception.Message
        };

        if (exception is ArgumentException || exception is InvalidOperationException)
        {
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Title = "Bad Request";
            problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);

        return true;
    }
}

public class IdempotencyMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<IdempotencyMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method != HttpMethods.Post)
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyValues))
        {
            await next(context);
            return;
        }

        var idempotencyKey = idempotencyKeyValues.ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await next(context);
            return;
        }

        var cacheKey = $"Idempotency_{idempotencyKey}";

        if (cache.TryGetValue(cacheKey, out byte[]? cachedResponse) && cachedResponse != null)
        {
            logger.LogInformation("Idempotency cache hit for key: {Key}", idempotencyKey);
            context.Response.StatusCode = StatusCodes.Status200OK; // Assumes cached is successful
            context.Response.ContentType = "application/json";
            await context.Response.Body.WriteAsync(cachedResponse, 0, cachedResponse.Length);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await next(context);

        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            var responseBytes = responseBody.ToArray();
            cache.Set(cacheKey, responseBytes, TimeSpan.FromHours(24));
        }

        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }
}