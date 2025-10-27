using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class PurchaseOrderLine : Entity, ITenantScopedEntity
{
    public int PurchaseOrderId { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public int VariantId { get; set; }

    public ProductVariant? Variant { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? Discount { get; set; }

    public int? TaxRateId { get; set; }

    public TaxRate? TaxRate { get; set; }

    public decimal TotalLine { get; set; }

    public int TenantId { get; set; }

    public Tenant? Tenant { get; set; }
}
