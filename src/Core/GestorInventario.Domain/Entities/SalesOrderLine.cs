using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class SalesOrderLine : Entity, ITenantScopedEntity
{
    public int SalesOrderId { get; set; }

    public SalesOrder? SalesOrder { get; set; }

    public int VariantId { get; set; }

    public ProductVariant? Variant { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? Discount { get; set; }

    public int? TaxRateId { get; set; }

    public TaxRate? TaxRate { get; set; }

    public decimal TotalLine { get; set; }

    public ICollection<SalesOrderAllocation> Allocations { get; set; } = new List<SalesOrderAllocation>();

    public ICollection<ShipmentLine> ShipmentLines { get; set; } = new List<ShipmentLine>();

    public int TenantId { get; set; }

    public Tenant? Tenant { get; set; }
}
