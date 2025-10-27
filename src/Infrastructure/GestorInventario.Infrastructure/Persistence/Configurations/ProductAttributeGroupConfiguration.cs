using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class ProductAttributeGroupConfiguration : IEntityTypeConfiguration<ProductAttributeGroup>
{
    public void Configure(EntityTypeBuilder<ProductAttributeGroup> builder)
    {
        builder.ToTable("ProductAttributeGroups");

        builder.Property(group => group.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(group => group.Slug)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(group => group.Description)
            .HasMaxLength(250);

        builder.Property(group => group.AllowCustomValues)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(group => new { group.TenantId, group.Slug })
            .IsUnique();

        builder.HasMany(group => group.Values)
            .WithOne(value => value.Group)
            .HasForeignKey(value => value.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
