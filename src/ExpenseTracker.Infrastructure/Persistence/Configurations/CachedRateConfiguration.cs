using ExpenseTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

public class CachedRateConfiguration : IEntityTypeConfiguration<CachedRate>
{
    public void Configure(EntityTypeBuilder<CachedRate> builder)
    {
        builder.ToTable("cached_rates");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.FromCurrency)
            .HasConversion(ExpenseTracker.Infrastructure.Persistence.Converters.DomainConverters.CurrencyCodeConverter)
            .HasColumnName("from_currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.ToCurrency)
            .HasConversion(ExpenseTracker.Infrastructure.Persistence.Converters.DomainConverters.CurrencyCodeConverter)
            .HasColumnName("to_currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Rate)
            .HasColumnName("rate")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.FetchedAtUtc)
            .HasColumnName("fetched_at_utc")
            .IsRequired();

        builder.Property(x => x.Source)
            .HasColumnName("source")
            .HasMaxLength(50)
            .IsRequired();

        // Used for TTL lookup
        builder.HasIndex(x => new { x.FromCurrency, x.ToCurrency, x.FetchedAtUtc });
    }
}
