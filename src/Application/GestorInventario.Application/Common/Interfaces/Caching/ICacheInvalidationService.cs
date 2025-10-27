namespace GestorInventario.Application.Common.Interfaces.Caching;

public interface ICacheInvalidationService
{
    Task InvalidateRegionAsync(string region, CancellationToken cancellationToken);
}
