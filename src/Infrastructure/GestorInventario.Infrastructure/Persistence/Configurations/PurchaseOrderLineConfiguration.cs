using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines");

        builder.Property(line => line.Quantity)
            .HasPrecision(18, 4);

        builder.Property(line => line.UnitPrice)
            .HasPrecision(18, 4);

        builder.Property(line => line.Discount)
            .HasPrecision(18, 4);

        builder.Property(line => line.TotalLine)
            .HasPrecision(18, 4);

        builder.Property(line => line.TenantId)
            .IsRequired();

        builder.HasIndex(line => new { line.TenantId, line.PurchaseOrderId });

        builder.HasOne(line => line.PurchaseOrder)
            .WithMany(order => order.Lines)
            .HasForeignKey(line => line.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(line => line.Variant)
            .WithMany(variant => variant.PurchaseOrderLines)
            .HasForeignKey(line => line.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(line => line.TaxRate)
            .WithMany(tax => tax.PurchaseOrderLines)
            .HasForeignKey(line => line.TaxRateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
