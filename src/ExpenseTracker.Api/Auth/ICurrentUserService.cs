using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Auth;

namespace ExpenseTracker.Api.Auth;

/// <summary>
/// Resolves the currently authenticated user per request. Null when anonymous.
/// Reads from the authenticated <see cref="System.Security.Claims.ClaimsPrincipal"/>.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>True when the request carries a valid access token.</summary>
    bool IsAuthenticated { get; }

    /// <summary>The user id claim; null when anonymous.</summary>
    UserId? UserId { get; }

    /// <summary>The user's email claim; empty when anonymous.</summary>
    string Email { get; }

    /// <summary>The active tenant id from the JWT; null when no tenant is active.</summary>
    TenantId? ActiveTenantId { get; }

    /// <summary>The user's role in the active tenant; null when no tenant active.</summary>
    TenantRole? RoleInActiveTenant { get; }
}

/// <summary>
/// Scoped service that reads the current user's claims from the HTTP context.
/// Wires to <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>.
/// </summary>
public sealed class HttpContextCurrentUserService : ICurrentUserService
{
    private readonly System.Security.Claims.ClaimsPrincipal? _principal;

    public HttpContextCurrentUserService(Microsoft.AspNetCore.Http.IHttpContextAccessor accessor)
    {
        _principal = accessor.HttpContext?.User;
    }

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated == true;

    public UserId? UserId
    {
        get
        {
            var claim = _principal?.FindFirst(JwtAccessTokenService.UserIdClaimType)?.Value
                ?? _principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(claim, out var g) ? new UserId(g) : null;
        }
    }

    public string Email => _principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;

    public TenantId? ActiveTenantId
    {
        get
        {
            var claim = _principal?.FindFirst(JwtAccessTokenService.TenantIdClaimType)?.Value;
            return Guid.TryParse(claim, out var g) ? new TenantId(g) : null;
        }
    }

    public TenantRole? RoleInActiveTenant
    {
        get
        {
            var claim = _principal?.FindFirst(JwtAccessTokenService.TenantRoleClaimType)?.Value;
            return int.TryParse(claim, out var r) ? (TenantRole)r : null;
        }
    }
}