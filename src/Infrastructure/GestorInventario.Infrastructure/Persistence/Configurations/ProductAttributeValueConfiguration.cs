using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.ToTable("ProductAttributeValues");

        builder.Property(value => value.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(value => value.Description)
            .HasMaxLength(200);

        builder.Property(value => value.HexColor)
            .HasMaxLength(7);

        builder.Property(value => value.DisplayOrder)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(value => value.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(value => new { value.GroupId, value.Name })
            .IsUnique();
    }
}
