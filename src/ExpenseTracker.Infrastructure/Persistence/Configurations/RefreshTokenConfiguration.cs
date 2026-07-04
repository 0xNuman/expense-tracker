using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="RefreshToken"/>.</summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(t => t.Id);

        b.Property(t => t.Id)
            .HasConversion(DomainConverters.RefreshTokenIdConverter)
            .HasColumnName("id");

        b.Property(t => t.UserId)
            .HasConversion(DomainConverters.UserIdConverter)
            .HasColumnName("user_id")
            .IsRequired();

        b.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(88).IsRequired();
        b.Property(t => t.FamilyId).HasColumnName("family_id").IsRequired();
        b.Property(t => t.IssuedAtUtc).HasColumnName("issued_at_utc").IsRequired();
        b.Property(t => t.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        b.Property(t => t.RevokedAtUtc).HasColumnName("revoked_at_utc");
        b.Property(t => t.ReplacedById)
            .HasConversion(DomainConverters.NullableRefreshTokenIdConverter)
            .HasColumnName("replaced_by_id");
        b.Property(t => t.DeviceLabel).HasColumnName("device_label").HasMaxLength(120);
        b.Property(t => t.LastSeenIp).HasColumnName("last_seen_ip").HasMaxLength(64);
        b.Property(t => t.LastSeenAtUtc).HasColumnName("last_seen_at_utc").IsRequired();

        b.Ignore(t => t.Events);

        b.HasIndex(t => t.TokenHash).IsUnique();
        b.HasIndex(t => t.UserId);
        b.HasIndex(t => t.FamilyId);
        b.HasIndex(t => t.ExpiresAtUtc);
    }
}