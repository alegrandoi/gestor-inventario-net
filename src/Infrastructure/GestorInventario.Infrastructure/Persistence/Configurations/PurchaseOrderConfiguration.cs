using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(order => order.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(order => order.TotalAmount)
            .HasPrecision(18, 4);

        builder.Property(order => order.Notes)
            .HasMaxLength(200);

        builder.HasIndex(order => order.OrderDate);

        builder.Property(order => order.TenantId)
            .IsRequired();

        builder.HasOne(order => order.Supplier)
            .WithMany(supplier => supplier.PurchaseOrders)
            .HasForeignKey(order => order.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
