using GestorInventario.Domain.Enums;

namespace GestorInventario.Application.SalesOrders.Models;

public record ShipmentSummaryDto(
    int Id,
    int WarehouseId,
    string WarehouseName,
    ShipmentStatus Status,
    DateTime CreatedAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    int? CarrierId,
    string? CarrierName,
    string? TrackingNumber,
    decimal? TotalWeight,
    DateTime? EstimatedDeliveryDate);
