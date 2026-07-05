using ExpenseTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

public class FXSnapshotConfiguration : IEntityTypeConfiguration<FXSnapshot>
{
    public void Configure(EntityTypeBuilder<FXSnapshot> builder)
    {
        builder.ToTable("fx_snapshots");

        builder.HasKey(x => x.SnapshotId);
        builder.Property(x => x.SnapshotId).HasColumnName("snapshot_id");

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
            
        builder.Property(x => x.Method)
            .HasColumnName("method")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Snapshots lookup by (From, To, FetchedAtUtc)
        builder.HasIndex(x => new { x.FromCurrency, x.ToCurrency, x.FetchedAtUtc });
    }
}
