using ExpenseTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure;

public sealed class RecurringRuleConfiguration : IEntityTypeConfiguration<RecurringRule>
{
    public void Configure(EntityTypeBuilder<RecurringRule> builder)
    {
        builder.ToTable("recurring_rules");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
               .HasConversion(id => id.Value, val => new RecurringRuleId(val))
               .HasColumnName("id");

        builder.Property(x => x.TenantId)
               .HasConversion(id => id.Value, val => new TenantId(val))
               .HasColumnName("tenant_id")
               .IsRequired();

        builder.Property(x => x.Name)
               .HasColumnName("name")
               .HasMaxLength(80)
               .IsRequired();

        builder.Property(x => x.Enabled).HasColumnName("enabled");
        builder.Property(x => x.RuleKind).HasColumnName("rule_kind").HasConversion<string>();
        builder.Property(x => x.Cadence).HasColumnName("cadence").HasConversion<string>();
        builder.Property(x => x.Interval).HasColumnName("interval");
        
        builder.Property(x => x.DaysOfWeek).HasColumnName("days_of_week");
        builder.Property(x => x.DayOfMonth).HasColumnName("day_of_month");
        builder.Property(x => x.MonthOfYear).HasColumnName("month_of_year");
        
        builder.Property(x => x.StartDateUtc).HasColumnName("start_date_utc");
        builder.Property(x => x.EndDateUtc).HasColumnName("end_date_utc");
        builder.Property(x => x.NextRunUtc).HasColumnName("next_run_utc");

        builder.Property(x => x.AccountId)
               .HasConversion(id => id.Value, val => new AccountId(val))
               .HasColumnName("account_id")
               .IsRequired();

        builder.Property(x => x.CounterpartAccountId)
               .HasConversion(ExpenseTracker.Infrastructure.Persistence.Converters.DomainConverters.NullableAccountIdConverter)
               .HasColumnName("counterpart_account_id");

        builder.Property(x => x.CategoryId)
               .HasConversion(ExpenseTracker.Infrastructure.Persistence.Converters.DomainConverters.NullableCategoryIdConverter)
               .HasColumnName("category_id");

        builder.ComplexProperty(x => x.AmountAccountCurrency, m =>
        {
            m.Property(p => p.Amount).HasColumnName("amount_account_currency").HasColumnType("numeric(18,4)");
            m.Property(p => p.Currency).HasColumnName("currency").HasConversion(ExpenseTracker.Infrastructure.Persistence.Converters.DomainConverters.CurrencyCodeConverter).HasMaxLength(3);
        });

        builder.Property(x => x.FxSnapshotId).HasColumnName("fx_snapshot_id");
        builder.Property(x => x.MemoPattern).HasColumnName("memo_pattern");
        
        builder.Property(x => x.Tags)
               .HasColumnName("tags")
               .HasColumnType("jsonb");

        builder.Property(x => x.AutoPost).HasColumnName("auto_post");
        builder.Property(x => x.GraceDays).HasColumnName("grace_days");
        builder.Property(x => x.LastRunAt).HasColumnName("last_run_at");
        
        builder.Property(x => x.LastRunTxnId)
               .HasConversion(ExpenseTracker.Infrastructure.Persistence.Converters.DomainConverters.NullableTransactionIdConverter)
               .HasColumnName("last_run_txn_id");
               
        builder.Property(x => x.Completed).HasColumnName("completed");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.NextRunUtc);
    }
}
