using GestorInventario.Application.Common.Messaging;
using GestorInventario.Domain.Enums;

namespace GestorInventario.Application.SalesOrders.IntegrationEvents;

public sealed record SalesOrderChangedIntegrationEvent(
    int OrderId,
    SalesOrderStatus Status,
    string CustomerName,
    decimal TotalAmount,
    string ChangeType,
    DateTime OccurredAt) : IIntegrationEvent
{
    public string EventName => "salesorder.changed";
}
