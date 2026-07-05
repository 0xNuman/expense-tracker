using ExpenseTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CategoryId(value))
            .IsRequired();

        builder.Property(c => c.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
            
        builder.Property(c => c.ParentId)
            .HasConversion(id => id.HasValue ? id.Value.Value : (Guid?)null, value => value.HasValue ? new CategoryId(value.Value) : null);

        builder.Property(c => c.Name)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(c => c.Kind)
            .IsRequired();

        builder.Property(c => c.Icon)
            .HasMaxLength(50);

        builder.Property(c => c.Color)
            .HasMaxLength(20);

        builder.Property(c => c.SortOrder)
            .IsRequired();

        builder.Property(c => c.IsArchived)
            .IsRequired();

        builder.Property(c => c.Notes)
            .HasMaxLength(500);

        // A category's Name should be unique among its siblings (within a tenant)
        builder.HasIndex(c => new { c.TenantId, c.ParentId, c.Name })
            .IsUnique();
    }
}
