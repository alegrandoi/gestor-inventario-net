using GestorInventario.Domain.Abstractions;
using GestorInventario.Domain.Enums;

namespace GestorInventario.Domain.Entities;

public class SalesOrder : TenantEntity, IAggregateRoot
{
    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public DateTime OrderDate { get; set; }

    public SalesOrderStatus Status { get; set; }

    public string? ShippingAddress { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public int? CarrierId { get; set; }

    public Carrier? Carrier { get; set; }

    public DateTime? EstimatedDeliveryDate { get; set; }

    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();

    public int? ShippingRateId { get; set; }

    public ShippingRate? ShippingRate { get; set; }

    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
