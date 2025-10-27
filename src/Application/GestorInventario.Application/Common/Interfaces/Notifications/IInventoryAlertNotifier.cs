using GestorInventario.Application.Inventory.Notifications;
using GestorInventario.Application.SalesOrders.Notifications;

namespace GestorInventario.Application.Common.Interfaces.Notifications;

public interface IInventoryAlertNotifier
{
    Task NotifyInventoryAdjustedAsync(InventoryAdjustmentNotification notification, CancellationToken cancellationToken);

    Task NotifySalesOrderChangedAsync(SalesOrderChangeNotification notification, CancellationToken cancellationToken);
}
