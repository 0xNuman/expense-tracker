using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="User"/> aggregate.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);

        b.Property(u => u.Id)
            .HasConversion(DomainConverters.UserIdConverter)
            .HasColumnName("id");

        b.Property(u => u.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        b.Property(u => u.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(254).IsRequired();
        b.HasIndex(u => u.NormalizedEmail).IsUnique();

        b.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(120).IsRequired();

        b.Property(u => u.PreferredBaseCurrency)
            .HasConversion(DomainConverters.CurrencyCodeConverter)
            .HasColumnName("preferred_base_currency")
            .HasMaxLength(3)
            .IsRequired();

        b.Property(u => u.TimeZone).HasColumnName("time_zone").HasMaxLength(64).IsRequired();
        b.Property(u => u.PreferredLocale).HasColumnName("preferred_locale").HasMaxLength(16).IsRequired();
        b.Property(u => u.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        b.Property(u => u.LastLoginAtUtc).HasColumnName("last_login_at_utc");
        b.Property(u => u.IsPending).HasColumnName("is_pending").IsRequired();
        b.Property(u => u.EmailConfirmed).HasColumnName("email_confirmed").IsRequired();

        b.Ignore(u => u.Events);

        b.Property<DateTimeOffset?>("ModifiedAtUtc").HasColumnName("modified_at_utc");
        b.Property<UserId?>("ModifiedByUserId")
            .HasConversion(DomainConverters.NullableUserIdConverter)
            .HasColumnName("modified_by_user_id");
    }
}