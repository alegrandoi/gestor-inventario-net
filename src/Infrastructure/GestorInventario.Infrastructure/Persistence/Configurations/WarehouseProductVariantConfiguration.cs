using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class WarehouseProductVariantConfiguration : IEntityTypeConfiguration<WarehouseProductVariant>
{
    public void Configure(EntityTypeBuilder<WarehouseProductVariant> builder)
    {
        builder.ToTable("WarehouseProductVariants");

        builder.Property(link => link.MinimumQuantity)
            .HasPrecision(18, 4)
            .HasDefaultValue(0);

        builder.Property(link => link.TargetQuantity)
            .HasPrecision(18, 4)
            .HasDefaultValue(0);

        builder.HasIndex(link => new { link.TenantId, link.WarehouseId, link.VariantId })
            .IsUnique();

        builder.Property(link => link.TenantId)
            .IsRequired();

        builder.HasOne(link => link.Warehouse)
            .WithMany(warehouse => warehouse.WarehouseProductVariants)
            .HasForeignKey(link => link.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Variant)
            .WithMany(variant => variant.WarehouseProductVariants)
            .HasForeignKey(link => link.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
