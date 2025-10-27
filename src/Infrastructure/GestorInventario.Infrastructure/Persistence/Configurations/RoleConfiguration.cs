using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.Property(role => role.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(role => role.Description)
            .HasMaxLength(200);

        builder.HasIndex(role => role.Name)
            .IsUnique();
    }
}
