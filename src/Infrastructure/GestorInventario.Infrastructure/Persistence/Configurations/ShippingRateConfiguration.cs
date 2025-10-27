using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class ShippingRateConfiguration : IEntityTypeConfiguration<ShippingRate>
{
    public void Configure(EntityTypeBuilder<ShippingRate> builder)
    {
        builder.ToTable("ShippingRates");

        builder.Property(rate => rate.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(rate => rate.BaseCost)
            .HasPrecision(18, 4);

        builder.Property(rate => rate.CostPerWeight)
            .HasPrecision(18, 4);

        builder.Property(rate => rate.CostPerDistance)
            .HasPrecision(18, 4);

        builder.Property(rate => rate.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(rate => rate.Description)
            .HasMaxLength(200);
    }
}
