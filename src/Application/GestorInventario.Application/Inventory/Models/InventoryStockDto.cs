namespace GestorInventario.Application.Inventory.Models;

public record InventoryStockDto(
    int Id,
    int VariantId,
    int WarehouseId,
    decimal Quantity,
    decimal ReservedQuantity,
    decimal MinStockLevel,
    string VariantSku,
    string ProductName,
    string WarehouseName);
