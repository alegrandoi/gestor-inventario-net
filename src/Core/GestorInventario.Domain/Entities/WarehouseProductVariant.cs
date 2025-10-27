using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class WarehouseProductVariant : TenantEntity
{
    public int WarehouseId { get; set; }

    public Warehouse? Warehouse { get; set; }

    public int VariantId { get; set; }

    public ProductVariant? Variant { get; set; }

    public decimal MinimumQuantity { get; set; }

    public decimal TargetQuantity { get; set; }
}
