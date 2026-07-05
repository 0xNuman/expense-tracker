using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="Transaction"/>.</summary>
public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.ToTable("transactions");
        b.HasKey(t => t.Id);

        b.Property(t => t.Id)
            .HasConversion(DomainConverters.TransactionIdConverter)
            .HasColumnName("id");

        b.Property(t => t.TenantId)
            .HasConversion(DomainConverters.TenantIdConverter)
            .HasColumnName("tenant_id")
            .IsRequired();

        b.Property(t => t.AccountId)
            .HasConversion(DomainConverters.AccountIdConverter)
            .HasColumnName("account_id")
            .IsRequired();

        b.Property(t => t.CategoryId)
            .HasConversion(DomainConverters.NullableCategoryIdConverter)
            .HasColumnName("category_id");

        b.Property(t => t.Type).HasColumnName("type").HasConversion<int>().IsRequired();
        b.Property(t => t.Amount).HasColumnName("amount").HasColumnType("numeric(18,4)").IsRequired();

        b.Property(t => t.Currency)
            .HasConversion(DomainConverters.CurrencyCodeConverter)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        b.Property(t => t.Memo).HasColumnName("memo").HasMaxLength(500);
        b.Property(t => t.OccurredOn).HasColumnName("occurred_on").IsRequired();
        b.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        b.Property(t => t.CreatedByUserId)
            .HasConversion(DomainConverters.UserIdConverter)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        b.Property(t => t.IsVoided).HasColumnName("is_voided").IsRequired();
        b.Property(t => t.VoidedAtUtc).HasColumnName("voided_at_utc");

        b.Ignore(t => t.Events);

        b.Property(t => t.ImportBatchId)
            .HasConversion(DomainConverters.NullableImportBatchIdConverter)
            .HasColumnName("import_batch_id");

        b.Property(t => t.Tags)
            .HasColumnName("tags"); // PostgreSQL natively maps List<string> to text[]

        b.HasIndex(t => new { t.TenantId, t.AccountId });
        b.HasIndex(t => t.OccurredOn);
        b.HasIndex(t => t.TenantId);
        b.HasIndex(t => t.ImportBatchId);
    }
}