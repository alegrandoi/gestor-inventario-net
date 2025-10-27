namespace GestorInventario.Application.Shipments.Models;

public record ShipmentLineDto(
    int Id,
    int SalesOrderLineId,
    int? SalesOrderAllocationId,
    int VariantId,
    string VariantSku,
    string ProductName,
    decimal Quantity,
    decimal? Weight,
    int WarehouseId,
    string WarehouseName);
