using GestorInventario.Domain.Enums;

namespace GestorInventario.Application.Shipments.Models;

public record ShipmentDto(
    int Id,
    int SalesOrderId,
    ShipmentStatus Status,
    int WarehouseId,
    string WarehouseName,
    int? CarrierId,
    string? CarrierName,
    string? TrackingNumber,
    DateTime CreatedAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime? EstimatedDeliveryDate,
    decimal? TotalWeight,
    string? Notes,
    IReadOnlyCollection<ShipmentLineDto> Lines,
    IReadOnlyCollection<ShipmentEventDto> Events);
