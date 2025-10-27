using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class DemandHistory : TenantEntity
{
    public int VariantId { get; set; }

    public ProductVariant? Variant { get; set; }

    public DateTime Date { get; set; }

    public decimal Quantity { get; set; }

    public decimal? ForecastQuantity { get; set; }
}
