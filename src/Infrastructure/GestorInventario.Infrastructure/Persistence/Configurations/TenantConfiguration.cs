using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.DefaultCulture)
            .HasMaxLength(32);

        builder.Property(t => t.DefaultCurrency)
            .HasMaxLength(8);

        builder.HasIndex(t => t.Code).IsUnique();

        builder.HasMany(t => t.Branches)
            .WithOne(b => b.Tenant!)
            .HasForeignKey(b => b.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
