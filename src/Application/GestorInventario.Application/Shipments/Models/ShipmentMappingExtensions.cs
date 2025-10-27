using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.Shipments.Models;

public static class ShipmentMappingExtensions
{
    public static ShipmentDto ToDto(this Shipment shipment)
    {
        var lines = shipment.Lines
            .Select(line => new ShipmentLineDto(
                line.Id,
                line.SalesOrderLineId,
                line.SalesOrderAllocationId,
                line.SalesOrderLine?.VariantId ?? 0,
                line.SalesOrderLine?.Variant?.Sku ?? string.Empty,
                line.SalesOrderLine?.Variant?.Product?.Name ?? string.Empty,
                line.Quantity,
                line.Weight,
                shipment.WarehouseId,
                shipment.Warehouse?.Name ?? string.Empty))
            .ToList();

        var events = shipment.Events
            .OrderByDescending(shipmentEvent => shipmentEvent.EventDate)
            .ThenByDescending(shipmentEvent => shipmentEvent.CreatedAt)
            .Select(shipmentEvent => new ShipmentEventDto(
                shipmentEvent.Id,
                shipmentEvent.Status,
                shipmentEvent.Location,
                shipmentEvent.Description,
                shipmentEvent.EventDate,
                shipmentEvent.CreatedAt))
            .ToList();

        return new ShipmentDto(
            shipment.Id,
            shipment.SalesOrderId,
            shipment.Status,
            shipment.WarehouseId,
            shipment.Warehouse?.Name ?? string.Empty,
            shipment.CarrierId,
            shipment.Carrier?.Name,
            shipment.TrackingNumber,
            shipment.CreatedAt,
            shipment.ShippedAt,
            shipment.DeliveredAt,
            shipment.EstimatedDeliveryDate,
            shipment.TotalWeight,
            shipment.Notes,
            lines,
            events);
    }
}
