using GestorInventario.Domain.Enums;

namespace GestorInventario.Application.SalesOrders.Models;

public record SalesOrderAllocationDto(
    int Id,
    int WarehouseId,
    string WarehouseName,
    decimal Quantity,
    decimal FulfilledQuantity,
    SalesOrderAllocationStatus Status,
    DateTime CreatedAt,
    DateTime? ShippedAt,
    DateTime? ReleasedAt);
