using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        builder.ToTable("TaxRates");

        builder.Property(tax => tax.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(tax => tax.Rate)
            .HasPrecision(5, 2);

        builder.Property(tax => tax.Region)
            .HasMaxLength(50);

        builder.Property(tax => tax.Description)
            .HasMaxLength(200);

        builder.HasIndex(tax => new { tax.Name, tax.Region })
            .IsUnique();
    }
}
