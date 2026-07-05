using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="Account"/>.</summary>
public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.ToTable("accounts");
        b.HasKey(a => a.Id);

        b.Property(a => a.Id)
            .HasConversion(DomainConverters.AccountIdConverter)
            .HasColumnName("id");

        b.Property(a => a.TenantId)
            .HasConversion(DomainConverters.TenantIdConverter)
            .HasColumnName("tenant_id")
            .IsRequired();

        b.Property(a => a.Name).HasColumnName("name").HasMaxLength(60).IsRequired();
        b.Property(a => a.Type).HasColumnName("type").HasConversion<int>().IsRequired();

        b.Property(a => a.Currency)
            .HasConversion(DomainConverters.CurrencyCodeConverter)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        b.Property(a => a.OpeningBalance)
            .HasConversion(DomainConverters.MoneyToAmountConverter)
            .HasColumnName("opening_balance")
            .HasColumnType("numeric(18,4)")
            .IsRequired();
        b.Property(a => a.OpenedAtUtc).HasColumnName("opened_at_utc").IsRequired();
        b.Property(a => a.ClosedAtUtc).HasColumnName("closed_at_utc");
        b.Property(a => a.IsArchived).HasColumnName("is_archived").IsRequired();

        b.Ignore(a => a.Events);

        b.HasIndex(a => new { a.TenantId, a.Name }).IsUnique();
        b.HasIndex(a => a.TenantId);
    }
}