using ExpenseTracker.Domain;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>
/// Resolves the tenant identifier scoped to the current HTTP request.
/// In MVP the active tenant is supplied by the JWT claim <c>tenant_id</c>;
/// until auth is wired this defaults to <see cref="TenantId.Empty"/>.
/// </summary>
public interface ITenantContext
{
    /// <summary>The tenant id active for the current request scope.</summary>
    TenantId ActiveTenantId { get; }
}

/// <summary>Placeholder implementation pre-auth; replaced once JWT claims land.</summary>
public sealed class AmbientTenantContext : ITenantContext
{
    public TenantId ActiveTenantId { get; set; }
}

/// <summary>Strongly-typed representation of "no tenant active".</summary>
public static class TenantContextDefaults
{
    public static readonly TenantId Empty = new(Guid.Empty);
}