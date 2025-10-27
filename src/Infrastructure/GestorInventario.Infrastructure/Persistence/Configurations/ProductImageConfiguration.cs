using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        builder.Property(image => image.ImageUrl)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(image => image.AltText)
            .HasMaxLength(200);

        builder.Property(image => image.TenantId)
            .IsRequired();

        builder.HasIndex(image => new { image.TenantId, image.ProductId });

        builder.HasOne(image => image.Product)
            .WithMany(product => product.Images)
            .HasForeignKey(image => image.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
