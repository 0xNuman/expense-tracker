using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

public sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> b)
    {
        b.HasKey(t => t.Id);

        b.Property(t => t.Id)
            .HasConversion(DomainConverters.TransferIdConverter)
            .HasColumnName("id");

        b.Property(t => t.TenantId)
            .HasConversion(DomainConverters.TenantIdConverter)
            .HasColumnName("tenant_id")
            .IsRequired();

        b.Property(t => t.SourceAccountId)
            .HasConversion(DomainConverters.AccountIdConverter)
            .HasColumnName("source_account_id")
            .IsRequired();
            
        b.Property(t => t.DestinationAccountId)
            .HasConversion(DomainConverters.AccountIdConverter)
            .HasColumnName("destination_account_id")
            .IsRequired();

        b.Property(t => t.SourceAmount).HasColumnName("source_amount").HasColumnType("numeric(18,4)").IsRequired();
        b.Property(t => t.SourceCurrency)
            .HasConversion(DomainConverters.CurrencyCodeConverter)
            .HasColumnName("source_currency")
            .HasMaxLength(3)
            .IsRequired();

        b.Property(t => t.DestinationAmount).HasColumnName("destination_amount").HasColumnType("numeric(18,4)").IsRequired();
        b.Property(t => t.DestinationCurrency)
            .HasConversion(DomainConverters.CurrencyCodeConverter)
            .HasColumnName("destination_currency")
            .HasMaxLength(3)
            .IsRequired();

        b.Property(t => t.FxSnapshot)
            .HasConversion(
                v => v.HasValue ? System.Text.Json.JsonSerializer.Serialize(v.Value, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => string.IsNullOrEmpty(v) ? (FXRate?)null : System.Text.Json.JsonSerializer.Deserialize<FXRate>(v, (System.Text.Json.JsonSerializerOptions?)null)
            )
            .HasColumnName("fx_snapshot")
            .HasColumnType("jsonb");

        b.Property(t => t.OccurredOnUtc).HasColumnName("occurred_on_utc").IsRequired();
        b.Property(t => t.Memo).HasColumnName("memo").HasMaxLength(500);
        b.Property(t => t.IsVoided).HasColumnName("is_voided").IsRequired();
        
        b.Property(t => t.VoidedById)
            .HasConversion(DomainConverters.NullableUserIdConverter)
            .HasColumnName("voided_by_id");
        b.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        b.Ignore(t => t.Events);

        b.HasIndex(t => t.TenantId);
        b.HasIndex(t => t.SourceAccountId);
        b.HasIndex(t => t.DestinationAccountId);
    }
}
