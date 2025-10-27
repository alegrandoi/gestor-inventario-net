using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("Variants");

        builder.Property(variant => variant.Sku)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(variant => variant.Attributes)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(variant => variant.Price)
            .HasPrecision(18, 4);

        builder.Property(variant => variant.Barcode)
            .HasMaxLength(50);

        builder.HasIndex(variant => new { variant.TenantId, variant.Sku }).IsUnique();

        builder.Property(variant => variant.TenantId)
            .IsRequired();

        builder.HasOne(variant => variant.Product)
            .WithMany(product => product.Variants)
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
