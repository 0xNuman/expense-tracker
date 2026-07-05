using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="PasskeyCredential"/>.</summary>
public sealed class PasskeyCredentialConfiguration : IEntityTypeConfiguration<PasskeyCredential>
{
    public void Configure(EntityTypeBuilder<PasskeyCredential> b)
    {
        b.ToTable("passkey_credentials");
        b.HasKey(c => c.Id);

        b.Property(c => c.Id)
            .HasConversion(DomainConverters.PasskeyCredentialIdConverter)
            .HasColumnName("id");

        b.Property(c => c.UserId)
            .HasConversion(DomainConverters.UserIdConverter)
            .HasColumnName("user_id")
            .IsRequired();

        b.Property(c => c.CredentialIdBase64Url).HasColumnName("credential_id").HasMaxLength(256).IsRequired();
        b.Property(c => c.PublicKey).HasColumnName("public_key").IsRequired();
        b.Property(c => c.SignCount).HasColumnName("sign_count").IsRequired();
        b.Property(c => c.DeviceLabel).HasColumnName("device_label").HasMaxLength(120).IsRequired();
        b.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        b.Property(c => c.LastUsedAtUtc).HasColumnName("last_used_at_utc");

        b.HasIndex(c => c.CredentialIdBase64Url).IsUnique();
        b.HasIndex(c => c.UserId);
    }
}