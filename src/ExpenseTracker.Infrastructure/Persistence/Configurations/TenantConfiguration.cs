using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="Tenant"/> aggregate.</summary>
public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("tenants");
        b.HasKey(t => t.Id);

        b.Property(t => t.Id)
            .HasConversion(DomainConverters.TenantIdConverter)
            .HasColumnName("id");

        b.Property(t => t.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(t => t.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();

        b.Property(t => t.CreatedByUserId)
            .HasConversion(DomainConverters.UserIdConverter)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        b.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        b.Ignore(t => t.Events);

        b.HasMany(t => t.Memberships)
            .WithOne()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Navigation(t => t.Memberships)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}