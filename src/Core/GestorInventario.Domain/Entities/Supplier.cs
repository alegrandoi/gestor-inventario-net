using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class Supplier : TenantEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Notes { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
