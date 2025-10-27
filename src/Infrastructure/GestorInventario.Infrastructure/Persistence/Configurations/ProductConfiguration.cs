using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.Property(product => product.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(product => product.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(product => product.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(product => product.DefaultPrice)
            .HasPrecision(18, 4);

        builder.Property(product => product.WeightKg)
            .HasPrecision(18, 4)
            .HasDefaultValue(0);

        builder.Property(product => product.WidthCm)
            .HasPrecision(18, 4);

        builder.Property(product => product.HeightCm)
            .HasPrecision(18, 4);

        builder.Property(product => product.LengthCm)
            .HasPrecision(18, 4);

        builder.Property(product => product.LeadTimeDays);

        builder.Property(product => product.SafetyStock)
            .HasPrecision(18, 4);

        builder.Property(product => product.ReorderPoint)
            .HasPrecision(18, 4);

        builder.Property(product => product.ReorderQuantity)
            .HasPrecision(18, 4);

        builder.Property(product => product.RequiresSerialTracking)
            .HasDefaultValue(false);

        builder.HasIndex(product => new { product.TenantId, product.Code }).IsUnique();

        builder.Property(product => product.TenantId)
            .IsRequired();

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(product => product.TaxRate)
            .WithMany(taxRate => taxRate.Products)
            .HasForeignKey(product => product.TaxRateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
