using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class InventoryStock : TenantEntity
{
    public int VariantId { get; set; }

    public ProductVariant? Variant { get; set; }

    public int WarehouseId { get; set; }

    public Warehouse? Warehouse { get; set; }

    public decimal Quantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal MinStockLevel { get; set; }
}
