using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class ShipmentEvent : TenantEntity
{
    public int ShipmentId { get; set; }

    public Shipment? Shipment { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Location { get; set; }

    public string? Description { get; set; }

    public DateTime EventDate { get; set; }
}
