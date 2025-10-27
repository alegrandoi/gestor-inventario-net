using System.Text.Json;
using GestorInventario.Application.Analytics.Models;
using GestorInventario.Application.Common.Caching;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Interfaces.Caching;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Application.Analytics.Queries;

public record GetInventoryDashboardQuery() : IRequest<InventoryDashboardDto>;

public class GetInventoryDashboardQueryHandler : IRequestHandler<GetInventoryDashboardQuery, InventoryDashboardDto>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IDistributedCache cache;
    private readonly ICacheKeyRegistry cacheKeyRegistry;
    private readonly ILogger<GetInventoryDashboardQueryHandler> logger;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
    };

    public GetInventoryDashboardQueryHandler(
        IGestorInventarioDbContext context,
        IDistributedCache cache,
        ICacheKeyRegistry cacheKeyRegistry,
        ILogger<GetInventoryDashboardQueryHandler> logger)
    {
        this.context = context;
        this.cache = cache;
        this.cacheKeyRegistry = cacheKeyRegistry;
        this.logger = logger;
    }

    public async Task<InventoryDashboardDto> Handle(GetInventoryDashboardQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.InventoryDashboard();
        var cached = await cache.TryGetStringAsync(cacheKey, logger, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedValue = JsonSerializer.Deserialize<InventoryDashboardDto>(cached, SerializerOptions);
            if (cachedValue is not null)
            {
                return cachedValue;
            }
        }

        var totalProducts = await context.Products.CountAsync(cancellationToken).ConfigureAwait(false);
        var activeProducts = await context.Products.CountAsync(product => product.IsActive, cancellationToken).ConfigureAwait(false);

        var inventory = await context.InventoryStocks
            .Include(stock => stock.Variant)
                .ThenInclude(variant => variant!.Product)
            .Include(stock => stock.Warehouse)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        decimal totalInventoryValue = 0;
        var reorderAlerts = new List<ReorderAlertDto>();

        foreach (var stock in inventory)
        {
            if (stock.Variant is null)
            {
                continue;
            }

            var unitPrice = stock.Variant.Price ?? stock.Variant.Product?.DefaultPrice ?? 0;
            totalInventoryValue += (stock.Quantity - stock.ReservedQuantity) * unitPrice;

            if (stock.Quantity <= stock.MinStockLevel)
            {
                reorderAlerts.Add(new ReorderAlertDto(
                    stock.Variant.Id,
                    stock.Variant.Product?.Name ?? string.Empty,
                    stock.Variant.Sku,
                    stock.Quantity,
                    stock.MinStockLevel,
                    stock.Warehouse?.Name ?? "Sin almacén"));
            }
        }

        var lowStockVariants = reorderAlerts.Count;

        var salesLines = await context.SalesOrderLines
            .Include(line => line.SalesOrder)
            .Include(line => line.Variant)
                .ThenInclude(variant => variant!.Product)
            .AsNoTracking()
            .Where(line => line.SalesOrder != null &&
                           (line.SalesOrder.Status == SalesOrderStatus.Confirmed ||
                            line.SalesOrder.Status == SalesOrderStatus.Shipped ||
                            line.SalesOrder.Status == SalesOrderStatus.Delivered))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var validSalesLines = salesLines
            .Where(line => line.Variant is not null && line.Variant.Product is not null && line.SalesOrder is not null)
            .Select(line => new
            {
                Variant = line.Variant!,
                Product = line.Variant!.Product!,
                Order = line.SalesOrder!,
                line.Quantity,
                line.TotalLine
            })
            .ToList();

        var topSellingProducts = validSalesLines
            .GroupBy(entry => new { entry.Variant.ProductId, entry.Product.Name })
            .Select(group => new TopSellingProductDto(
                group.Key.ProductId,
                group.Key.Name,
                group.Sum(entry => entry.Quantity),
                group.Sum(entry => entry.TotalLine)))
            .OrderByDescending(product => product.Quantity)
            .Take(5)
            .ToList();

        var monthlySales = validSalesLines
            .GroupBy(entry => new { entry.Order.OrderDate.Year, entry.Order.OrderDate.Month })
            .Select(group => new SalesTrendPointDto(
                group.Key.Year,
                group.Key.Month,
                group.Sum(entry => entry.TotalLine)))
            .OrderBy(point => point.Year)
            .ThenBy(point => point.Month)
            .ToList();

        var dashboard = new InventoryDashboardDto(
            totalProducts,
            activeProducts,
            Math.Round(totalInventoryValue, 2),
            lowStockVariants,
            reorderAlerts
                .OrderBy(alert => alert.Quantity - alert.MinStockLevel)
                .Take(10)
                .ToList(),
            topSellingProducts,
            monthlySales);

        var serialized = JsonSerializer.Serialize(dashboard, SerializerOptions);
        await cache.TrySetStringAsync(cacheKey, serialized, CacheOptions, logger, cancellationToken).ConfigureAwait(false);
        await cacheKeyRegistry.TryRegisterKeyAsync(CacheRegions.InventoryDashboard, cacheKey, logger, cancellationToken).ConfigureAwait(false);

        return dashboard;
    }
}
