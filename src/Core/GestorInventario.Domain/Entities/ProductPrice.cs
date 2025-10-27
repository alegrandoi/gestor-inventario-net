using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class ProductPrice : Entity, ITenantScopedEntity
{
    public int PriceListId { get; set; }

    public PriceList? PriceList { get; set; }

    public int VariantId { get; set; }

    public ProductVariant? Variant { get; set; }

    public decimal Price { get; set; }

    public int TenantId { get; set; }

    public Tenant? Tenant { get; set; }
}
