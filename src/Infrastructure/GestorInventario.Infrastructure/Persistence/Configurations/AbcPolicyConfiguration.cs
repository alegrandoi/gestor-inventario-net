using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class AbcPolicyConfiguration : IEntityTypeConfiguration<AbcPolicy>
{
    public void Configure(EntityTypeBuilder<AbcPolicy> builder)
    {
        builder.ToTable("AbcPolicies");

        builder.Property(policy => policy.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(policy => policy.Description)
            .HasMaxLength(500);

        builder.Property(policy => policy.ThresholdA)
            .HasPrecision(5, 4);

        builder.Property(policy => policy.ThresholdB)
            .HasPrecision(5, 4);

        builder.Property(policy => policy.ServiceLevelA)
            .HasPrecision(5, 4);

        builder.Property(policy => policy.ServiceLevelB)
            .HasPrecision(5, 4);

        builder.Property(policy => policy.ServiceLevelC)
            .HasPrecision(5, 4);
    }
}
