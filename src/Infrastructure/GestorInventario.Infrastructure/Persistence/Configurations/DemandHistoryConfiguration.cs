using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class DemandHistoryConfiguration : IEntityTypeConfiguration<DemandHistory>
{
    public void Configure(EntityTypeBuilder<DemandHistory> builder)
    {
        builder.ToTable("DemandHistory");

        builder.Property(history => history.Quantity)
            .HasPrecision(18, 4);

        builder.Property(history => history.ForecastQuantity)
            .HasPrecision(18, 4);

        builder.Property(history => history.TenantId)
            .IsRequired();

        builder.HasIndex(history => new { history.TenantId, history.VariantId, history.Date });

        builder.HasOne(history => history.Variant)
            .WithMany(variant => variant.DemandHistory)
            .HasForeignKey(history => history.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
