using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="TenantMembership"/>.</summary>
public sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> b)
    {
        b.ToTable("tenant_memberships");
        b.HasKey(m => m.Id);

        b.Property(m => m.Id)
            .HasConversion(DomainConverters.TenantMembershipIdConverter)
            .HasColumnName("id");

        b.Property(m => m.TenantId)
            .HasConversion(DomainConverters.TenantIdConverter)
            .HasColumnName("tenant_id")
            .IsRequired();

        b.Property(m => m.UserId)
            .HasConversion(DomainConverters.UserIdConverter)
            .HasColumnName("user_id")
            .IsRequired();

        b.Property(m => m.Role).HasColumnName("role").IsRequired();
        b.Property(m => m.Role).HasConversion<int>();

        b.Property(m => m.InvitedByUserId)
            .HasConversion(DomainConverters.UserIdConverter)
            .HasColumnName("invited_by_user_id")
            .IsRequired();

        b.Property(m => m.JoinedAtUtc).HasColumnName("joined_at_utc").IsRequired();

        b.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique();
        b.HasIndex(m => m.UserId);
    }
}