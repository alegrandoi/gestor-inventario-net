using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.SalesOrders.Models;

public static class SalesOrderMappingExtensions
{
    public static SalesOrderDto ToDto(this SalesOrder order)
    {
        var lines = order.Lines
            .Select(line => new SalesOrderLineDto(
                line.Id,
                line.VariantId,
                line.Quantity,
                line.UnitPrice,
                line.Discount,
                line.TaxRateId,
                line.TotalLine,
                line.Variant?.Sku ?? string.Empty,
                line.Variant?.Product?.Name ?? string.Empty,
                line.Allocations
                    .Select(allocation => new SalesOrderAllocationDto(
                        allocation.Id,
                        allocation.WarehouseId,
                        allocation.Warehouse?.Name ?? string.Empty,
                        allocation.Quantity,
                        allocation.FulfilledQuantity,
                        allocation.Status,
                        allocation.CreatedAt,
                        allocation.ShippedAt,
                        allocation.ReleasedAt))
                    .OrderBy(allocation => allocation.WarehouseName)
                    .ToList()))
            .ToList();

        var shipments = order.Shipments
            .Select(shipment => new ShipmentSummaryDto(
                shipment.Id,
                shipment.WarehouseId,
                shipment.Warehouse?.Name ?? string.Empty,
                shipment.Status,
                shipment.CreatedAt,
                shipment.ShippedAt,
                shipment.DeliveredAt,
                shipment.CarrierId,
                shipment.Carrier?.Name,
                shipment.TrackingNumber,
                shipment.TotalWeight,
                shipment.EstimatedDeliveryDate))
            .OrderBy(shipment => shipment.CreatedAt)
            .ToList();

        var totalOrdered = lines.Sum(line => line.Quantity);
        var totalFulfilled = order.Lines.Sum(line => line.Allocations.Sum(allocation => allocation.FulfilledQuantity));
        var fulfillmentRate = totalOrdered == 0 ? 0 : Math.Round(totalFulfilled / totalOrdered, 4);

        return new SalesOrderDto(
            order.Id,
            order.CustomerId,
            order.OrderDate,
            order.Status,
            order.ShippingAddress,
            order.TotalAmount,
            order.Currency,
            order.Notes,
            order.CarrierId,
            order.Carrier?.Name,
            order.EstimatedDeliveryDate,
            lines,
            order.Customer?.Name ?? string.Empty,
            fulfillmentRate,
            shipments);
    }
}
