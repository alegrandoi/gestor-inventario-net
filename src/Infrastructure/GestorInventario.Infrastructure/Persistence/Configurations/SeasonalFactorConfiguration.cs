using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class SeasonalFactorConfiguration : IEntityTypeConfiguration<SeasonalFactor>
{
    public void Configure(EntityTypeBuilder<SeasonalFactor> builder)
    {
        builder.ToTable("SeasonalFactors");

        builder.Property(factor => factor.Sequence)
            .IsRequired();

        builder.Property(factor => factor.Factor)
            .HasPrecision(18, 6);

        builder.Property(factor => factor.EffectiveFrom)
            .HasColumnType("date");

        builder.Property(factor => factor.EffectiveTo)
            .HasColumnType("date");

        builder.Property(factor => factor.TenantId)
            .IsRequired();

        builder.HasIndex(factor => new { factor.TenantId, factor.VariantId, factor.Interval, factor.Sequence, factor.EffectiveFrom });

        builder.HasOne(factor => factor.Variant)
            .WithMany(variant => variant.SeasonalFactors)
            .HasForeignKey(factor => factor.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
