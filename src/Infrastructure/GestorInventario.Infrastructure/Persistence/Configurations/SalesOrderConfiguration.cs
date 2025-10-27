using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders");

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(order => order.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(order => order.TotalAmount)
            .HasPrecision(18, 4);

        builder.Property(order => order.ShippingAddress)
            .HasMaxLength(200);

        builder.Property(order => order.Notes)
            .HasMaxLength(200);

        builder.Property(order => order.EstimatedDeliveryDate);

        builder.HasIndex(order => order.OrderDate);

        builder.Property(order => order.TenantId)
            .IsRequired();

        builder.HasOne(order => order.Customer)
            .WithMany(customer => customer.SalesOrders)
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(order => order.ShippingRate)
            .WithMany(rate => rate.SalesOrders)
            .HasForeignKey(order => order.ShippingRateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(order => order.Carrier)
            .WithMany(carrier => carrier.SalesOrders)
            .HasForeignKey(order => order.CarrierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
