using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");

        builder.Property(shipment => shipment.Status)
            .HasConversion<int>();

        builder.Property(shipment => shipment.TotalWeight)
            .HasPrecision(18, 4);

        builder.Property(shipment => shipment.TrackingNumber)
            .HasMaxLength(100);

        builder.Property(shipment => shipment.Notes)
            .HasMaxLength(500);

        builder.Property(shipment => shipment.TenantId)
            .IsRequired();

        builder.HasIndex(shipment => new { shipment.TenantId, shipment.SalesOrderId });

        builder.HasOne(shipment => shipment.SalesOrder)
            .WithMany(order => order.Shipments)
            .HasForeignKey(shipment => shipment.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(shipment => shipment.Warehouse)
            .WithMany(warehouse => warehouse.Shipments)
            .HasForeignKey(shipment => shipment.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(shipment => shipment.Carrier)
            .WithMany(carrier => carrier.Shipments)
            .HasForeignKey(shipment => shipment.CarrierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
