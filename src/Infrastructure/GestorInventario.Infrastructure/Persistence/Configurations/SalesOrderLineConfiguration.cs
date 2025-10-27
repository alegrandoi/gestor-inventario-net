using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable("SalesOrderLines");

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

        builder.HasIndex(line => new { line.TenantId, line.SalesOrderId });

        builder.HasOne(line => line.SalesOrder)
            .WithMany(order => order.Lines)
            .HasForeignKey(line => line.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(line => line.Variant)
            .WithMany(variant => variant.SalesOrderLines)
            .HasForeignKey(line => line.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(line => line.TaxRate)
            .WithMany(tax => tax.SalesOrderLines)
            .HasForeignKey(line => line.TaxRateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
