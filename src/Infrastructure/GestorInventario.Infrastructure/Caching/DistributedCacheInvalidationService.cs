using GestorInventario.Application.Common.Interfaces.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Infrastructure.Caching;

public class DistributedCacheInvalidationService : ICacheInvalidationService
{
    private readonly IDistributedCache cache;
    private readonly ICacheKeyRegistry cacheKeyRegistry;
    private readonly ILogger<DistributedCacheInvalidationService> logger;

    public DistributedCacheInvalidationService(
        IDistributedCache cache,
        ICacheKeyRegistry cacheKeyRegistry,
        ILogger<DistributedCacheInvalidationService> logger)
    {
        this.cache = cache;
        this.cacheKeyRegistry = cacheKeyRegistry;
        this.logger = logger;
    }

    public async Task InvalidateRegionAsync(string region, CancellationToken cancellationToken)
    {
        var keys = await cacheKeyRegistry.GetKeysAsync(region, cancellationToken).ConfigureAwait(false);
        if (keys.Count == 0)
        {
            return;
        }

        foreach (var key in keys)
        {
            await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("Invalidated cache entry {CacheKey} in region {Region}", key, region);
        }

        await cacheKeyRegistry.ClearRegionAsync(region, cancellationToken).ConfigureAwait(false);
    }
}
