using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class ShipmentEventConfiguration : IEntityTypeConfiguration<ShipmentEvent>
{
    public void Configure(EntityTypeBuilder<ShipmentEvent> builder)
    {
        builder.ToTable("ShipmentEvents");

        builder.Property(shipmentEvent => shipmentEvent.Status)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(shipmentEvent => shipmentEvent.Location)
            .HasMaxLength(200);

        builder.Property(shipmentEvent => shipmentEvent.Description)
            .HasMaxLength(500);

        builder.Property(shipmentEvent => shipmentEvent.TenantId)
            .IsRequired();

        builder.HasIndex(shipmentEvent => new { shipmentEvent.TenantId, shipmentEvent.ShipmentId, shipmentEvent.EventDate });

        builder.HasOne(shipmentEvent => shipmentEvent.Shipment)
            .WithMany(shipment => shipment.Events)
            .HasForeignKey(shipmentEvent => shipmentEvent.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
