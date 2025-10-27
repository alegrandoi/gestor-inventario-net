using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class ShipmentLine : TenantEntity
{
    public int ShipmentId { get; set; }

    public Shipment? Shipment { get; set; }

    public int SalesOrderLineId { get; set; }

    public SalesOrderLine? SalesOrderLine { get; set; }

    public int? SalesOrderAllocationId { get; set; }

    public SalesOrderAllocation? SalesOrderAllocation { get; set; }

    public decimal Quantity { get; set; }

    public decimal? Weight { get; set; }
}
