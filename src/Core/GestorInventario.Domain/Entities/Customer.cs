using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class Customer : TenantEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Notes { get; set; }

    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
