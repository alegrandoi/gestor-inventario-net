using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class SalesOrderAllocationConfiguration : IEntityTypeConfiguration<SalesOrderAllocation>
{
    public void Configure(EntityTypeBuilder<SalesOrderAllocation> builder)
    {
        builder.ToTable("SalesOrderAllocations");

        builder.Property(allocation => allocation.Quantity)
            .HasPrecision(18, 4);

        builder.Property(allocation => allocation.FulfilledQuantity)
            .HasPrecision(18, 4);

        builder.Property(allocation => allocation.Status)
            .HasConversion<int>();

        builder.HasOne(allocation => allocation.SalesOrderLine)
            .WithMany(line => line.Allocations)
            .HasForeignKey(allocation => allocation.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(allocation => allocation.Warehouse)
            .WithMany(warehouse => warehouse.SalesOrderAllocations)
            .HasForeignKey(allocation => allocation.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(allocation => allocation.TenantId)
            .IsRequired();
    }
}
