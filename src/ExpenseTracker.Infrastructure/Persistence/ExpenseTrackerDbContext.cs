using ExpenseTracker.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Expense Tracker persistence layer.
/// Backed by PostgreSQL via Npgsql. Tenant-scoped entities (everything that
/// belongs to a tenant except the <see cref="Tenant"/> aggregate itself) carry
/// a global query filter; the active tenant value supplied by the host's
/// <see cref="ITenantContext"/> applies automatically per request.
/// </summary>
public class ExpenseTrackerDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public ExpenseTrackerDbContext(DbContextOptions<ExpenseTrackerDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasskeyCredential> PasskeyCredentials => Set<PasskeyCredential>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<RecurringRule> RecurringRules => Set<RecurringRule>();
    public DbSet<RecurringExecutionLog> RecurringExecutionLogs => Set<RecurringExecutionLog>();
    public DbSet<CachedRate> CachedRates => Set<CachedRate>();
    public DbSet<FXSnapshot> FXSnapshots => Set<FXSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpenseTrackerDbContext).Assembly);

        // Tenant-scoped global query filters. Entities other than Tenant itself
        // that expose a TenantId property get filtered by the active tenant.
        modelBuilder.Entity<TenantMembership>()
            .HasQueryFilter(m => m.TenantId == _tenantContext.ActiveTenantId);

        modelBuilder.Entity<Account>()
            .HasQueryFilter(a => a.TenantId == _tenantContext.ActiveTenantId);

        modelBuilder.Entity<Transaction>()
            .HasQueryFilter(t => t.TenantId == _tenantContext.ActiveTenantId);

        modelBuilder.Entity<Category>()
            .HasQueryFilter(c => c.TenantId == _tenantContext.ActiveTenantId);

        modelBuilder.Entity<Transfer>()
            .HasQueryFilter(t => t.TenantId == _tenantContext.ActiveTenantId);

        modelBuilder.Entity<RecurringRule>()
            .HasQueryFilter(r => r.TenantId == _tenantContext.ActiveTenantId);
    }
}