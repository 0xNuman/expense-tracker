using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence;

namespace ExpenseTracker.Api.Auth;

/// <summary>
/// Scoped tenant context that resolves the active tenant from the authenticated
/// user's JWT claim. Falls back to <see cref="TenantContextDefaults.Empty"/>
/// when no tenant is active (pre-auth or anonymous requests).
/// </summary>
public sealed class RequestTenantContext : ITenantContext
{
    public TenantId ActiveTenantId { get; }

    public RequestTenantContext(ICurrentUserService currentUser)
    {
        ActiveTenantId = currentUser.ActiveTenantId ?? TenantContextDefaults.Empty;
    }
}