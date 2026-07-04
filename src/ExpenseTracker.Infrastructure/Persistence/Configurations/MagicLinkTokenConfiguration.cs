using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="MagicLinkToken"/>.</summary>
public sealed class MagicLinkTokenConfiguration : IEntityTypeConfiguration<MagicLinkToken>
{
    public void Configure(EntityTypeBuilder<MagicLinkToken> b)
    {
        b.ToTable("magic_link_tokens");
        b.HasKey(t => t.Id);

        b.Property(t => t.Id)
            .HasConversion(DomainConverters.MagicLinkTokenConverter)
            .HasColumnName("id");

        b.Property(t => t.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        b.Property(t => t.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(254).IsRequired();
        b.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(88).IsRequired();
        b.Property(t => t.IssuedAtUtc).HasColumnName("issued_at_utc").IsRequired();
        b.Property(t => t.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        b.Property(t => t.ConsumedAtUtc).HasColumnName("consumed_at_utc");
        b.Property(t => t.IssuedFromIp).HasColumnName("issued_from_ip").HasMaxLength(64);

        b.Property(t => t.UserId)
            .HasConversion(DomainConverters.NullableUserIdConverter)
            .HasColumnName("user_id");

        b.Ignore(t => t.Events);

        b.HasIndex(t => t.NormalizedEmail);
        b.HasIndex(t => t.TokenHash).IsUnique();
        b.HasIndex(t => t.ExpiresAtUtc);
    }
}