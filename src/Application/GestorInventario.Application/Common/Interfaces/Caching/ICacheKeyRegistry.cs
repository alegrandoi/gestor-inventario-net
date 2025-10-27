namespace GestorInventario.Application.Common.Interfaces.Caching;

public interface ICacheKeyRegistry
{
    Task RegisterKeyAsync(string region, string cacheKey, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetKeysAsync(string region, CancellationToken cancellationToken);

    Task ClearRegionAsync(string region, CancellationToken cancellationToken);
}
