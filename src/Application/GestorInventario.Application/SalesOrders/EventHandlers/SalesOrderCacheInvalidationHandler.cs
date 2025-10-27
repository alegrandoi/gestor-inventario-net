using GestorInventario.Application.Common.Caching;
using GestorInventario.Application.Common.Interfaces.Caching;
using GestorInventario.Application.SalesOrders.Events;
using MediatR;

namespace GestorInventario.Application.SalesOrders.EventHandlers;

public class SalesOrderCacheInvalidationHandler :
    INotificationHandler<SalesOrderCreatedDomainEvent>,
    INotificationHandler<SalesOrderStatusChangedDomainEvent>
{
    private readonly ICacheInvalidationService cacheInvalidationService;

    public SalesOrderCacheInvalidationHandler(ICacheInvalidationService cacheInvalidationService)
    {
        this.cacheInvalidationService = cacheInvalidationService;
    }

    public async Task Handle(SalesOrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        await cacheInvalidationService.InvalidateRegionAsync(CacheRegions.InventoryDashboard, cancellationToken).ConfigureAwait(false);
        await cacheInvalidationService.InvalidateRegionAsync(CacheRegions.LogisticsDashboard, cancellationToken).ConfigureAwait(false);
    }

    public async Task Handle(SalesOrderStatusChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        await cacheInvalidationService.InvalidateRegionAsync(CacheRegions.InventoryDashboard, cancellationToken).ConfigureAwait(false);
        await cacheInvalidationService.InvalidateRegionAsync(CacheRegions.LogisticsDashboard, cancellationToken).ConfigureAwait(false);
    }
}
