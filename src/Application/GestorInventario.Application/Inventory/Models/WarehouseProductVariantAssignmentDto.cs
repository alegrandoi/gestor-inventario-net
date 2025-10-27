namespace GestorInventario.Application.Inventory.Models;

public record WarehouseProductVariantAssignmentDto(
    int Id,
    int WarehouseId,
    int VariantId,
    decimal MinimumQuantity,
    decimal TargetQuantity,
    string VariantSku,
    string ProductName,
    string WarehouseName);
