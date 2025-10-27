using GestorInventario.Domain.Enums;
using MediatR;

namespace GestorInventario.Application.SalesOrders.Events;

public sealed record SalesOrderCreatedDomainEvent(
    int OrderId,
    SalesOrderStatus Status,
    string CustomerName,
    decimal TotalAmount,
    DateTime OrderDate) : INotification;
