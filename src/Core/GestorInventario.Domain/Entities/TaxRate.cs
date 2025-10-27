using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class TaxRate : AuditableEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public string? Region { get; set; }

    public string? Description { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();

    public ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();

    public ICollection<SalesOrderLine> SalesOrderLines { get; set; } = new List<SalesOrderLine>();
}
