using GestorInventario.Domain.Abstractions;
using GestorInventario.Domain.Enums;

namespace GestorInventario.Domain.Entities;

public class PurchaseOrder : TenantEntity, IAggregateRoot
{
    public int SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public DateTime OrderDate { get; set; }

    public PurchaseOrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}
