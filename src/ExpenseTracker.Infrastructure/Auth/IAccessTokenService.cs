using ExpenseTracker.Domain;

namespace ExpenseTracker.Infrastructure.Auth;

/// <summary>Claims bundle embedded in an access token.</summary>
public sealed record AccessTokenClaims(
    UserId UserId,
    string Email,
    TenantId? ActiveTenantId,
    TenantRole? RoleInActiveTenant,
    string[] Scopes);

/// <summary>Result of issuing an access token.</summary>
public record AccessTokenResult(string Token, DateTimeOffset ExpiresAtUtc);

/// <summary>
/// Issues and validates short-lived JWT access tokens signed with ECDSA P-256.
/// The signing key is supplied by the host via <see cref="JwtOptions"/>.
/// </summary>
public interface IAccessTokenService
{
    AccessTokenResult Issue(AccessTokenClaims claims, DateTimeOffset nowUtc);

    /// <summary>Validates a presented token and returns its claims. Null when invalid/expired.</summary>
    AccessTokenClaims? Validate(string token);
}