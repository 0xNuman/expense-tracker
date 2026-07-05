using ExpenseTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure;

public sealed class RecurringExecutionLogConfiguration : IEntityTypeConfiguration<RecurringExecutionLog>
{
    public void Configure(EntityTypeBuilder<RecurringExecutionLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
               .HasConversion(id => id.Value, val => new RecurringExecutionLogId(val))
               .HasColumnName("id");

        builder.Property(x => x.RuleId)
               .HasConversion(id => id.Value, val => new RecurringRuleId(val))
               .HasColumnName("rule_id")
               .IsRequired();
        builder.Property(x => x.PostedTxnId)
               .HasConversion(ExpenseTracker.Infrastructure.Persistence.Converters.DomainConverters.NullableTransactionIdConverter)
               .HasColumnName("posted_txn_id");

        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        builder.HasIndex(x => x.RuleId);
    }
}
