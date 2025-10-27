using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class ProductAttributeGroup : TenantEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool AllowCustomValues { get; set; }

    public ICollection<ProductAttributeValue> Values { get; set; } = new List<ProductAttributeValue>();
}
