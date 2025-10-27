using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class Warehouse : TenantEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? Description { get; set; }

    public ICollection<InventoryStock> InventoryStocks { get; set; } = new List<InventoryStock>();

    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public ICollection<SalesOrderAllocation> SalesOrderAllocations { get; set; } = new List<SalesOrderAllocation>();

    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    public ICollection<WarehouseProductVariant> WarehouseProductVariants { get; set; } = new List<WarehouseProductVariant>();
}
