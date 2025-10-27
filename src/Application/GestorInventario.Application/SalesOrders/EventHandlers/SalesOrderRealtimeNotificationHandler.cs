using GestorInventario.Application.Common.Interfaces.Notifications;
using GestorInventario.Application.SalesOrders.Events;
using GestorInventario.Application.SalesOrders.Notifications;
using MediatR;

namespace GestorInventario.Application.SalesOrders.EventHandlers;

public class SalesOrderRealtimeNotificationHandler :
    INotificationHandler<SalesOrderCreatedDomainEvent>,
    INotificationHandler<SalesOrderStatusChangedDomainEvent>
{
    private readonly IInventoryAlertNotifier notifier;

    public SalesOrderRealtimeNotificationHandler(IInventoryAlertNotifier notifier)
    {
        this.notifier = notifier;
    }

    public Task Handle(SalesOrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var payload = new SalesOrderChangeNotification(
            notification.OrderId,
            notification.Status,
            notification.CustomerName,
            notification.TotalAmount,
            "created",
            notification.OrderDate);

        return notifier.NotifySalesOrderChangedAsync(payload, cancellationToken);
    }

    public Task Handle(SalesOrderStatusChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        var payload = new SalesOrderChangeNotification(
            notification.OrderId,
            notification.NewStatus,
            notification.CustomerName,
            notification.TotalAmount,
            $"status:{notification.PreviousStatus}->{notification.NewStatus}",
            notification.UpdatedAt);

        return notifier.NotifySalesOrderChangedAsync(payload, cancellationToken);
    }
}
