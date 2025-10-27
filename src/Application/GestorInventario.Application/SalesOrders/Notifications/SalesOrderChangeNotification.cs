using GestorInventario.Domain.Enums;

namespace GestorInventario.Application.SalesOrders.Notifications;

public sealed record SalesOrderChangeNotification(
    int OrderId,
    SalesOrderStatus Status,
    string CustomerName,
    decimal TotalAmount,
    string ChangeType,
    DateTime OccurredAt);
