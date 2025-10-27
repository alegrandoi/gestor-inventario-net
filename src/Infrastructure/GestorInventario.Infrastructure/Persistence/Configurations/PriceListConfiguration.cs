using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.ToTable("PriceLists");

        builder.Property(priceList => priceList.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(priceList => priceList.Description)
            .HasMaxLength(200);

        builder.Property(priceList => priceList.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(priceList => priceList.TenantId)
            .IsRequired();

        builder.HasIndex(priceList => new { priceList.TenantId, priceList.Name })
            .IsUnique();
    }
}
