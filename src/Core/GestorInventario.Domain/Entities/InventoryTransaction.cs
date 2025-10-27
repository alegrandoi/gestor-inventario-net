using GestorInventario.Domain.Abstractions;
using GestorInventario.Domain.Enums;

namespace GestorInventario.Domain.Entities;

public class InventoryTransaction : TenantEntity
{
    public int VariantId { get; set; }

    public ProductVariant? Variant { get; set; }

    public int WarehouseId { get; set; }

    public Warehouse? Warehouse { get; set; }

    public InventoryTransactionType TransactionType { get; set; }

    public decimal Quantity { get; set; }

    public DateTime TransactionDate { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public int? UserId { get; set; }

    public User? User { get; set; }

    public string? Notes { get; set; }
}
