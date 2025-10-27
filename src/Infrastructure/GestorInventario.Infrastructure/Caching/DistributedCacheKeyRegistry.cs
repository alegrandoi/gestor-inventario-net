using System.Collections.Concurrent;
using System.Text.Json;
using GestorInventario.Application.Common.Interfaces.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace GestorInventario.Infrastructure.Caching;

public class DistributedCacheKeyRegistry : ICacheKeyRegistry
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(12)
    };

    private readonly IDistributedCache cache;

    public DistributedCacheKeyRegistry(IDistributedCache cache)
    {
        this.cache = cache;
    }

    public async Task RegisterKeyAsync(string region, string cacheKey, CancellationToken cancellationToken)
    {
        var locker = Locks.GetOrAdd(region, _ => new SemaphoreSlim(1, 1));
        await locker.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var regionKey = GetRegionKey(region);
            var stored = await cache.GetStringAsync(regionKey, cancellationToken).ConfigureAwait(false);
            var keys = string.IsNullOrWhiteSpace(stored)
                ? new HashSet<string>()
                : JsonSerializer.Deserialize<HashSet<string>>(stored, SerializerOptions) ?? new HashSet<string>();

            if (keys.Add(cacheKey))
            {
                var payload = JsonSerializer.Serialize(keys, SerializerOptions);
                await cache.SetStringAsync(regionKey, payload, CacheOptions, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            locker.Release();
        }
    }

    public async Task<IReadOnlyCollection<string>> GetKeysAsync(string region, CancellationToken cancellationToken)
    {
        var regionKey = GetRegionKey(region);
        var stored = await cache.GetStringAsync(regionKey, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return Array.Empty<string>();
        }

        var keys = JsonSerializer.Deserialize<HashSet<string>>(stored, SerializerOptions);
        return keys is null ? Array.Empty<string>() : keys.ToArray();
    }

    public Task ClearRegionAsync(string region, CancellationToken cancellationToken)
    {
        var regionKey = GetRegionKey(region);
        Locks.TryRemove(region, out var locker);
        locker?.Dispose();
        return cache.RemoveAsync(regionKey, cancellationToken);
    }

    private static string GetRegionKey(string region) => $"cache:registry:{region}";
}
