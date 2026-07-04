using ExpenseTracker.Api.Hal;

namespace ExpenseTracker.Api.Endpoints;

/// <summary>HAL root endpoint — the entry point clients walk to discover the API.</summary>
public static class Root
{
    /// <summary>Produces the HAL root document for <c>GET /api</c>.</summary>
    public static HalDocument GetRoot()
    {
        return new HalDocument()
            .WithCuries(new[]
            {
                new Link { Href = "/docs/rels/{rel}", Templated = true, Title = "Expense Tracker rel documentation" }
            })
            .WithLink("self", Link.Get("/api"))
            .WithLink("health-live", Link.Get("/health/live", "Liveness probe"))
            .WithLink("health-ready", Link.Get("/health/ready", "Readiness probe"))
            .WithLink("openapi", Link.Get("/api/openapi.json", "OpenAPI document"))
            .WithLink("et:auth", Link.Get("/api/auth", "Authentication endpoints (Phase 1)"))
            .WithLink("et:tenants", Link.Get("/api/tenants?filter=mine", "Tenants the caller belongs to (Phase 1)"))
            .WithLink("et:create-tenant", Link.Post("/api/tenants", "Create a new tenant workspace (Phase 1)"))
            .WithState("name", "Expense Tracker API")
            .WithState("version", "0.1.0")
            .WithState("phase", "walking-skeleton");
    }
}