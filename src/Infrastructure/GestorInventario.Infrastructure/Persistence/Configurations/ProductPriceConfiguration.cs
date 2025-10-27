using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class ProductPriceConfiguration : IEntityTypeConfiguration<ProductPrice>
{
    public void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        builder.ToTable("ProductPrices");

        builder.Property(price => price.Price)
            .HasPrecision(18, 4);

        builder.Property(price => price.TenantId)
            .IsRequired();

        builder.HasIndex(price => new { price.TenantId, price.PriceListId, price.VariantId })
            .IsUnique();

        builder.HasOne(price => price.PriceList)
            .WithMany(list => list.ProductPrices)
            .HasForeignKey(price => price.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(price => price.Variant)
            .WithMany(variant => variant.ProductPrices)
            .HasForeignKey(price => price.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
