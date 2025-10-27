using GestorInventario.Application.Inventory.Notifications;
using GestorInventario.Application.SalesOrders.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GestorInventario.Api.Hubs;

[Authorize]
public class InventoryHub : Hub<IInventoryHubClient>
{
}

public interface IInventoryHubClient
{
    Task InventoryAdjusted(InventoryAdjustmentNotification notification);

    Task SalesOrderChanged(SalesOrderChangeNotification notification);
}
