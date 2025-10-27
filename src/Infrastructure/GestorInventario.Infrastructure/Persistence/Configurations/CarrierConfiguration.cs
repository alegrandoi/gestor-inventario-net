using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class CarrierConfiguration : IEntityTypeConfiguration<Carrier>
{
    public void Configure(EntityTypeBuilder<Carrier> builder)
    {
        builder.ToTable("Carriers");

        builder.HasKey(carrier => carrier.Id);

        builder.Property(carrier => carrier.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(carrier => carrier.ContactName)
            .HasMaxLength(150);

        builder.Property(carrier => carrier.Email)
            .HasMaxLength(150);

        builder.Property(carrier => carrier.Phone)
            .HasMaxLength(50);

        builder.Property(carrier => carrier.TrackingUrl)
            .HasMaxLength(300);

        builder.Property(carrier => carrier.Notes)
            .HasMaxLength(500);

        builder.HasMany(carrier => carrier.SalesOrders)
            .WithOne(order => order.Carrier)
            .HasForeignKey(order => order.CarrierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(carrier => carrier.Shipments)
            .WithOne(shipment => shipment.Carrier)
            .HasForeignKey(shipment => shipment.CarrierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
