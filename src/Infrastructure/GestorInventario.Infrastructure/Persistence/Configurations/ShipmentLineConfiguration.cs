using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class ShipmentLineConfiguration : IEntityTypeConfiguration<ShipmentLine>
{
    public void Configure(EntityTypeBuilder<ShipmentLine> builder)
    {
        builder.ToTable("ShipmentLines");

        builder.Property(line => line.Quantity)
            .HasPrecision(18, 4);

        builder.Property(line => line.Weight)
            .HasPrecision(18, 4);

        builder.Property(line => line.TenantId)
            .IsRequired();

        builder.HasIndex(line => new { line.TenantId, line.ShipmentId });

        builder.HasOne(line => line.Shipment)
            .WithMany(shipment => shipment.Lines)
            .HasForeignKey(line => line.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(line => line.SalesOrderLine)
            .WithMany(line => line.ShipmentLines)
            .HasForeignKey(line => line.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(line => line.SalesOrderAllocation)
            .WithMany(allocation => allocation.ShipmentLines)
            .HasForeignKey(line => line.SalesOrderAllocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
