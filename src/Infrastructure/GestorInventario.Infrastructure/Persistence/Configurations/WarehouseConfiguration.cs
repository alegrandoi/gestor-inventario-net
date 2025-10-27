using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");

        builder.Property(warehouse => warehouse.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(warehouse => warehouse.Address)
            .HasMaxLength(200);

        builder.Property(warehouse => warehouse.Description)
            .HasMaxLength(200);

        builder.Property(warehouse => warehouse.TenantId)
            .IsRequired();
    }
}
