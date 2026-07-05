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
            .WithLink("et:auth", new Link
            {
                Href = "/api/auth/magic-link",
                Method = "POST",
                Title = "Request a magic-link login email"
            })
            .WithLink("et:auth-verify", new Link
            {
                Href = "/api/auth/magic-link/verify",
                Method = "POST",
                Title = "Verify a magic-link token and receive access + refresh tokens"
            })
            .WithLink("et:auth-refresh", new Link
            {
                Href = "/api/auth/refresh",
                Method = "POST",
                Title = "Rotate the refresh cookie and issue a new access token"
            })
            .WithLink("et:auth-switch-tenant", new Link
            {
                Href = "/api/auth/switch-tenant",
                Method = "POST",
                Title = "Switch the active tenant (requires authentication)"
            })
            .WithLink("et:passkey-begin-auth", new Link
            {
                Href = "/api/auth/passkeys/begin-auth",
                Method = "POST",
                Title = "Begin WebAuthn (passkey) sign-in"
            })
            .WithLink("et:tenants", Link.Get("/api/tenants?filter=mine", "Tenants the caller belongs to (Phase 1)"))
            .WithLink("et:create-tenant", Link.Post("/api/tenants", "Create a new tenant workspace (Phase 1)"))
            .WithLink("et:accounts", Link.Get("/api/accounts", "List accounts in the active tenant"))
            .WithLink("et:create-account", Link.Post("/api/accounts", "Create a new account in the active tenant"))
            .WithLink("et:transactions", Link.Get("/api/transactions", "List all transactions in the active tenant"))
            .WithState("name", "Expense Tracker API")
            .WithState("version", "0.1.0")
            .WithState("phase", "auth");
    }
}