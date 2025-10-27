using GestorInventario.Domain.Abstractions;
using GestorInventario.Domain.Enums;

namespace GestorInventario.Domain.Entities;

public class Shipment : TenantEntity, IAggregateRoot
{
    public int SalesOrderId { get; set; }

    public SalesOrder? SalesOrder { get; set; }

    public int WarehouseId { get; set; }

    public Warehouse? Warehouse { get; set; }

    public int? CarrierId { get; set; }

    public Carrier? Carrier { get; set; }

    public string? TrackingNumber { get; set; }

    public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? EstimatedDeliveryDate { get; set; }

    public decimal? TotalWeight { get; set; }

    public string? Notes { get; set; }

    public ICollection<ShipmentLine> Lines { get; set; } = new List<ShipmentLine>();

    public ICollection<ShipmentEvent> Events { get; set; } = new List<ShipmentEvent>();
}
