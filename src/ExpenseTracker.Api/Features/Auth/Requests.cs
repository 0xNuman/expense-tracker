namespace ExpenseTracker.Api.Features.Auth;

/// <summary>Request body for POST /api/auth/magic-link.</summary>
public sealed class MagicLinkRequest
{
    public string Email { get; init; } = string.Empty;
}

/// <summary>Request body for POST /api/auth/magic-link/verify.</summary>
public sealed class VerifyMagicLinkRequest
{
    public string Token { get; init; } = string.Empty;
}

/// <summary>Request body for POST /api/auth/switch-tenant.</summary>
public sealed class SwitchTenantRequest
{
    public string TenantId { get; init; } = string.Empty;
}

/// <summary>Response body for token-bearing endpoints (verify, refresh, switch-tenant).</summary>
public sealed class TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string TenantName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}