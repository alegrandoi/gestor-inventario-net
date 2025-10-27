using GestorInventario.Domain.Abstractions;
using GestorInventario.Domain.Enums;

namespace GestorInventario.Domain.Entities;

public class DemandAggregate : TenantEntity
{
    public int VariantId { get; set; }

    public ProductVariant? Variant { get; set; }

    public DateOnly PeriodStart { get; set; }

    public AggregationInterval Interval { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal? TotalRevenue { get; set; }

    public decimal? AverageLeadTimeDays { get; set; }
}
