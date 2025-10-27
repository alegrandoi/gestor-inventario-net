namespace GestorInventario.Application.Warehouses.Models;

using GestorInventario.Application.Inventory.Models;

public record WarehouseDto(
    int Id,
    string Name,
    string? Address,
    string? Description,
    IReadOnlyCollection<WarehouseProductVariantAssignmentDto> ProductVariants);
