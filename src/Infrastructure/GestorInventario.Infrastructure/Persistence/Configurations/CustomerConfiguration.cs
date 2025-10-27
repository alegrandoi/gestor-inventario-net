using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.Property(customer => customer.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasMaxLength(200);

        builder.Property(customer => customer.Phone)
            .HasMaxLength(50);

        builder.Property(customer => customer.Address)
            .HasMaxLength(200);

        builder.Property(customer => customer.Notes)
            .HasMaxLength(200);

        builder.Property(customer => customer.TenantId)
            .IsRequired();
    }
}
