using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class ProductImage : Entity, ITenantScopedEntity
{
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string? AltText { get; set; }

    public int TenantId { get; set; }

    public Tenant? Tenant { get; set; }
}
