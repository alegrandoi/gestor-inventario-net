using GestorInventario.Domain.Enums;
using MediatR;

namespace GestorInventario.Application.SalesOrders.Events;

public sealed record SalesOrderStatusChangedDomainEvent(
    int OrderId,
    SalesOrderStatus PreviousStatus,
    SalesOrderStatus NewStatus,
    decimal TotalAmount,
    string CustomerName,
    DateTime UpdatedAt) : INotification;
