using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class PriceList : TenantEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Currency { get; set; } = string.Empty;

    public ICollection<ProductPrice> ProductPrices { get; set; } = new List<ProductPrice>();
}
