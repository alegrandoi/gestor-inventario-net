using System.Linq;
using GestorInventario.Application.Inventory.Models;
using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.Warehouses.Models;

public static class WarehouseMappingExtensions
{
    public static WarehouseDto ToDto(this Warehouse warehouse) =>
        new(
            warehouse.Id,
            warehouse.Name,
            warehouse.Address,
            warehouse.Description,
            (warehouse.WarehouseProductVariants ?? Enumerable.Empty<WarehouseProductVariant>())
                .Select(variant => variant.ToAssignmentDto(warehouse))
                .ToList());

    public static WarehouseProductVariantAssignmentDto ToAssignmentDto(
        this WarehouseProductVariant assignment,
        Warehouse? parentWarehouse = null)
    {
        var variant = assignment.Variant;
        var product = variant?.Product;
        var warehouse = parentWarehouse ?? assignment.Warehouse;

        return new WarehouseProductVariantAssignmentDto(
            assignment.Id,
            assignment.WarehouseId,
            assignment.VariantId,
            assignment.MinimumQuantity,
            assignment.TargetQuantity,
            variant?.Sku ?? string.Empty,
            product?.Name ?? string.Empty,
            warehouse?.Name ?? string.Empty);
    }
}
