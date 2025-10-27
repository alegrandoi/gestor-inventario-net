using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class DemandAggregateConfiguration : IEntityTypeConfiguration<DemandAggregate>
{
    public void Configure(EntityTypeBuilder<DemandAggregate> builder)
    {
        builder.ToTable("DemandAggregates");

        builder.Property(aggregate => aggregate.PeriodStart)
            .HasColumnType("date");

        builder.Property(aggregate => aggregate.TotalQuantity)
            .HasPrecision(18, 4);

        builder.Property(aggregate => aggregate.TotalRevenue)
            .HasPrecision(18, 4);

        builder.Property(aggregate => aggregate.AverageLeadTimeDays)
            .HasPrecision(18, 4);

        builder.Property(aggregate => aggregate.TenantId)
            .IsRequired();

        builder.HasIndex(aggregate => new { aggregate.TenantId, aggregate.VariantId, aggregate.PeriodStart, aggregate.Interval })
            .IsUnique();

        builder.HasOne(aggregate => aggregate.Variant)
            .WithMany(variant => variant.DemandAggregates)
            .HasForeignKey(aggregate => aggregate.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
