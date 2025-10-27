using GestorInventario.Application.SalesOrders.Models;
using GestorInventario.Domain.Enums;

namespace GestorInventario.Application.SalesOrders.Models;

public record SalesOrderDto(
    int Id,
    int CustomerId,
    DateTime OrderDate,
    SalesOrderStatus Status,
    string? ShippingAddress,
    decimal TotalAmount,
    string Currency,
    string? Notes,
    int? CarrierId,
    string? CarrierName,
    DateTime? EstimatedDeliveryDate,
    IReadOnlyCollection<SalesOrderLineDto> Lines,
    string CustomerName,
    decimal FulfillmentRate,
    IReadOnlyCollection<ShipmentSummaryDto> Shipments);
