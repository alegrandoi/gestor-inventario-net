using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Interfaces.Notifications;
using GestorInventario.Application.Inventory.Notifications;
using MediatR;

namespace GestorInventario.Application.Inventory.EventHandlers;

public class InventoryAdjustedRealtimeNotificationHandler : INotificationHandler<InventoryAdjustedDomainEvent>
{
    private readonly IInventoryAlertNotifier notifier;

    public InventoryAdjustedRealtimeNotificationHandler(IInventoryAlertNotifier notifier)
    {
        this.notifier = notifier;
    }

    public Task Handle(InventoryAdjustedDomainEvent notification, CancellationToken cancellationToken)
    {
        var payload = new InventoryAdjustmentNotification(
            notification.VariantId,
            notification.VariantSku,
            notification.ProductName,
            notification.Adjustments,
            notification.TransactionType,
            notification.Quantity,
            notification.DestinationWarehouseId,
            notification.ReferenceType,
            notification.ReferenceId,
            notification.Notes,
            notification.OccurredAt);

        return notifier.NotifyInventoryAdjustedAsync(payload, cancellationToken);
    }
}
