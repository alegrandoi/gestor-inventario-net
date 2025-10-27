using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class InventoryStockConfiguration : IEntityTypeConfiguration<InventoryStock>
{
    public void Configure(EntityTypeBuilder<InventoryStock> builder)
    {
        builder.ToTable("InventoryStock");

        builder.Property(stock => stock.Quantity)
            .HasPrecision(18, 4);

        builder.Property(stock => stock.ReservedQuantity)
            .HasPrecision(18, 4);

        builder.Property(stock => stock.MinStockLevel)
            .HasPrecision(18, 4);

        builder.HasIndex(stock => new { stock.TenantId, stock.VariantId, stock.WarehouseId })
            .IsUnique();

        builder.Property(stock => stock.TenantId)
            .IsRequired();

        builder.HasOne(stock => stock.Variant)
            .WithMany(variant => variant.InventoryStocks)
            .HasForeignKey(stock => stock.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(stock => stock.Warehouse)
            .WithMany(warehouse => warehouse.InventoryStocks)
            .HasForeignKey(stock => stock.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
