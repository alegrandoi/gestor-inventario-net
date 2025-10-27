using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.Property(b => b.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(b => b.Locale)
            .HasMaxLength(16);

        builder.Property(b => b.TimeZone)
            .HasMaxLength(64);

        builder.Property(b => b.Currency)
            .HasMaxLength(8);

        builder.HasIndex(b => new { b.TenantId, b.Code })
            .IsUnique();
    }
}
