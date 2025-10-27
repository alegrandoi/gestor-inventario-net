using System.Globalization;

namespace GestorInventario.Application.Common.Caching;

public static class CacheKeys
{
    public static string ProductCatalog(string? searchTerm, int? categoryId, bool? isActive, int pageNumber, int pageSize)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? "" : searchTerm.Trim().ToLowerInvariant();
        var categoryPart = categoryId?.ToString(CultureInfo.InvariantCulture) ?? "all";
        var activePart = isActive.HasValue ? isActive.Value.ToString() : "all";
        var normalizedPageNumber = pageNumber > 0 ? pageNumber : 1;
        var normalizedPageSize = pageSize > 0 ? pageSize : 1;
        return $"cache:products:{normalizedSearch}:{categoryPart}:{activePart}:{normalizedPageNumber}:{normalizedPageSize}";
    }

    public static string InventoryDashboard() => "cache:dashboards:inventory";

    public static string LogisticsDashboard(int planningWindowDays) => $"cache:dashboards:logistics:{planningWindowDays}";
}
