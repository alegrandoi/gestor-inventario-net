using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class VariantAbcClassificationConfiguration : IEntityTypeConfiguration<VariantAbcClassification>
{
    public void Configure(EntityTypeBuilder<VariantAbcClassification> builder)
    {
        builder.ToTable("VariantAbcClassifications");

        builder.Property(classification => classification.Classification)
            .HasMaxLength(1)
            .IsRequired();

        builder.Property(classification => classification.AnnualConsumptionValue)
            .HasPrecision(18, 4);

        builder.Property(classification => classification.EffectiveFrom)
            .HasColumnType("date");

        builder.Property(classification => classification.EffectiveTo)
            .HasColumnType("date");

        builder.Property(classification => classification.TenantId)
            .IsRequired();

        builder.HasIndex(classification => new { classification.TenantId, classification.VariantId, classification.EffectiveFrom })
            .HasDatabaseName("IX_VariantAbcClassifications_Variant_EffectiveFrom");

        builder.HasOne(classification => classification.Variant)
            .WithMany(variant => variant.AbcClassifications)
            .HasForeignKey(classification => classification.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(classification => classification.Policy)
            .WithMany(policy => policy.Classifications)
            .HasForeignKey(classification => classification.AbcPolicyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
