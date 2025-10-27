using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.Property(supplier => supplier.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(supplier => supplier.ContactName)
            .HasMaxLength(150);

        builder.Property(supplier => supplier.Email)
            .HasMaxLength(200);

        builder.Property(supplier => supplier.Phone)
            .HasMaxLength(50);

        builder.Property(supplier => supplier.Address)
            .HasMaxLength(200);

        builder.Property(supplier => supplier.Notes)
            .HasMaxLength(200);

        builder.Property(supplier => supplier.TenantId)
            .IsRequired();
    }
}
