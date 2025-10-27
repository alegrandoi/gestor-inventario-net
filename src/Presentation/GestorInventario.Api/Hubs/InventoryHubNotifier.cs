using GestorInventario.Application.Common.Interfaces.Notifications;
using GestorInventario.Application.Inventory.Notifications;
using GestorInventario.Application.SalesOrders.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace GestorInventario.Api.Hubs;

public class InventoryHubNotifier : IInventoryAlertNotifier
{
    private readonly IHubContext<InventoryHub, IInventoryHubClient> hubContext;

    public InventoryHubNotifier(IHubContext<InventoryHub, IInventoryHubClient> hubContext)
    {
        this.hubContext = hubContext;
    }

    public Task NotifyInventoryAdjustedAsync(InventoryAdjustmentNotification notification, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All.InventoryAdjusted(notification);
    }

    public Task NotifySalesOrderChangedAsync(SalesOrderChangeNotification notification, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All.SalesOrderChanged(notification);
    }
}
