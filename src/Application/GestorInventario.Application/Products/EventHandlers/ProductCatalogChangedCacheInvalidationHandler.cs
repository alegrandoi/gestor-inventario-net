using GestorInventario.Application.Common.Caching;
using GestorInventario.Application.Common.Interfaces.Caching;
using GestorInventario.Application.Products.Events;
using MediatR;

namespace GestorInventario.Application.Products.EventHandlers;

public class ProductCatalogChangedCacheInvalidationHandler : INotificationHandler<ProductCatalogChangedDomainEvent>
{
    private readonly ICacheInvalidationService cacheInvalidationService;

    public ProductCatalogChangedCacheInvalidationHandler(ICacheInvalidationService cacheInvalidationService)
    {
        this.cacheInvalidationService = cacheInvalidationService;
    }

    public async Task Handle(ProductCatalogChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        await cacheInvalidationService.InvalidateRegionAsync(CacheRegions.ProductCatalog, cancellationToken).ConfigureAwait(false);
        await cacheInvalidationService.InvalidateRegionAsync(CacheRegions.InventoryDashboard, cancellationToken).ConfigureAwait(false);
        await cacheInvalidationService.InvalidateRegionAsync(CacheRegions.LogisticsDashboard, cancellationToken).ConfigureAwait(false);
    }
}
