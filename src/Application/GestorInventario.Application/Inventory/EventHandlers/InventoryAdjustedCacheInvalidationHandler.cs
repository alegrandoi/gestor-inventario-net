using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Caching;
using GestorInventario.Application.Common.Interfaces.Caching;
using MediatR;

namespace GestorInventario.Application.Inventory.EventHandlers;

public class InventoryAdjustedCacheInvalidationHandler : INotificationHandler<InventoryAdjustedDomainEvent>
{
    private readonly ICacheInvalidationService cacheInvalidationService;

    public InventoryAdjustedCacheInvalidationHandler(ICacheInvalidationService cacheInvalidationService)
    {
        this.cacheInvalidationService = cacheInvalidationService;
    }

    public async Task Handle(InventoryAdjustedDomainEvent notification, CancellationToken cancellationToken)
    {
        await cacheInvalidationService.InvalidateRegionAsync(CacheRegions.ProductCatalog, cancellationToken).ConfigureAwait(false);
        await cacheInvalidationService.InvalidateRegionAsync(CacheRegions.InventoryDashboard, cancellationToken).ConfigureAwait(false);
        await cacheInvalidationService.InvalidateRegionAsync(CacheRegions.LogisticsDashboard, cancellationToken).ConfigureAwait(false);
    }
}
