using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class ShippingRate : AuditableEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;

    public decimal BaseCost { get; set; }

    public decimal? CostPerWeight { get; set; }

    public decimal? CostPerDistance { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
