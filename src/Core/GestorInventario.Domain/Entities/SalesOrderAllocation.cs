using GestorInventario.Domain.Abstractions;
using GestorInventario.Domain.Enums;

namespace GestorInventario.Domain.Entities;

public class SalesOrderAllocation : TenantEntity
{
    public int SalesOrderLineId { get; set; }

    public SalesOrderLine? SalesOrderLine { get; set; }

    public int WarehouseId { get; set; }

    public Warehouse? Warehouse { get; set; }

    public decimal Quantity { get; set; }

    public decimal FulfilledQuantity { get; set; }

    public SalesOrderAllocationStatus Status { get; set; } = SalesOrderAllocationStatus.Reserved;

    public DateTime? ShippedAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public ICollection<ShipmentLine> ShipmentLines { get; set; } = new List<ShipmentLine>();
}
