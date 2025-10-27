using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class Carrier : TenantEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? TrackingUrl { get; set; }

    public string? Notes { get; set; }

    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();

    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
