using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using GestorInventario.Application.Analytics.Models;
using GestorInventario.Application.Common.Caching;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Interfaces.Caching;
using GestorInventario.Application.Inventory.Queries;
using GestorInventario.Application.SalesOrders.Models;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Application.Analytics.Queries;

public record GetLogisticsDashboardQuery(int PlanningWindowDays) : IRequest<LogisticsDashboardDto>;

public class GetLogisticsDashboardQueryHandler : IRequestHandler<GetLogisticsDashboardQuery, LogisticsDashboardDto>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IDistributedCache cache;
    private readonly ICacheKeyRegistry cacheKeyRegistry;
    private readonly ILogger<GetLogisticsDashboardQueryHandler> logger;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
    };

    public GetLogisticsDashboardQueryHandler(
        IGestorInventarioDbContext context,
        IDistributedCache cache,
        ICacheKeyRegistry cacheKeyRegistry,
        ILogger<GetLogisticsDashboardQueryHandler> logger)
    {
        this.context = context;
        this.cache = cache;
        this.cacheKeyRegistry = cacheKeyRegistry;
        this.logger = logger;
    }

    public async Task<LogisticsDashboardDto> Handle(GetLogisticsDashboardQuery request, CancellationToken cancellationToken)
    {
        var planningWindowDays = Math.Clamp(request.PlanningWindowDays, 7, 180);
        var cacheKey = CacheKeys.LogisticsDashboard(planningWindowDays);
        var cached = await cache.TryGetStringAsync(cacheKey, logger, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedValue = JsonSerializer.Deserialize<LogisticsDashboardDto>(cached, SerializerOptions);
            if (cachedValue is not null)
            {
                return cachedValue;
            }
        }

        var windowStart = DateTime.UtcNow.AddDays(-planningWindowDays);

        var shipments = await context.Shipments
            .AsNoTracking()
            .Where(shipment =>
                shipment.CreatedAt >= windowStart ||
                (shipment.ShippedAt.HasValue && shipment.ShippedAt.Value >= windowStart) ||
                (!shipment.DeliveredAt.HasValue && shipment.EstimatedDeliveryDate.HasValue &&
                    shipment.EstimatedDeliveryDate.Value >= windowStart))
            .Select(shipment => new
            {
                shipment.Id,
                shipment.Status,
                shipment.ShippedAt,
                shipment.DeliveredAt,
                shipment.EstimatedDeliveryDate,
                shipment.CreatedAt,
                shipment.CarrierId,
                CarrierName = shipment.Carrier != null ? shipment.Carrier.Name : null,
                shipment.TrackingNumber,
                shipment.TotalWeight,
                shipment.WarehouseId,
                WarehouseName = shipment.Warehouse != null ? shipment.Warehouse.Name : null
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalShipments = shipments.Count;
        var inTransitShipments = shipments.Count(shipment => shipment.Status == ShipmentStatus.InTransit);
        var deliveredShipments = shipments.Count(shipment => shipment.Status == ShipmentStatus.Delivered);

        var transitDurations = shipments
            .Where(shipment => shipment.ShippedAt.HasValue && shipment.DeliveredAt.HasValue)
            .Select(shipment => (shipment.DeliveredAt!.Value - shipment.ShippedAt!.Value).TotalDays)
            .Where(duration => duration >= 0)
            .ToList();

        var averageTransitDays = transitDurations.Count > 0
            ? Math.Round(transitDurations.Average(), 2)
            : 0;

        var deliveredWithEstimate = shipments
            .Where(shipment => shipment.Status == ShipmentStatus.Delivered && shipment.DeliveredAt.HasValue && shipment.EstimatedDeliveryDate.HasValue)
            .ToList();

        var onTimeDelivered = deliveredWithEstimate.Count(shipment => shipment.DeliveredAt!.Value <= shipment.EstimatedDeliveryDate!.Value);
        var onTimeDeliveryRate = deliveredWithEstimate.Count > 0
            ? Math.Round(onTimeDelivered / (double)deliveredWithEstimate.Count, 4)
            : 0d;

        var delayedShipments = shipments
            .Where(shipment => shipment.EstimatedDeliveryDate.HasValue && shipment.DeliveredAt.HasValue && shipment.DeliveredAt > shipment.EstimatedDeliveryDate)
            .Select(shipment => new
            {
                Shipment = shipment,
                Delay = (shipment.DeliveredAt!.Value - shipment.EstimatedDeliveryDate!.Value).TotalDays
            })
            .OrderByDescending(entry => entry.Delay)
            .Take(5)
            .Select(entry => new ShipmentSummaryDto(
                entry.Shipment.Id,
                entry.Shipment.WarehouseId,
                entry.Shipment.WarehouseName ?? string.Empty,
                entry.Shipment.Status,
                entry.Shipment.CreatedAt,
                entry.Shipment.ShippedAt,
                entry.Shipment.DeliveredAt,
                entry.Shipment.CarrierId,
                entry.Shipment.CarrierName,
                entry.Shipment.TrackingNumber,
                entry.Shipment.TotalWeight,
                entry.Shipment.EstimatedDeliveryDate))
            .ToList();

        var trendLookup = shipments
            .Select(shipment =>
            {
                var referenceDate = shipment.ShippedAt
                    ?? shipment.EstimatedDeliveryDate
                    ?? shipment.CreatedAt;

                if (referenceDate < windowStart)
                {
                    referenceDate = windowStart;
                }

                var date = DateOnly.FromDateTime(referenceDate.Date);
                return (date, shipment.Status);
            })
            .GroupBy(entry => entry.date)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Total = group.Count(),
                    Delivered = group.Count(entry => entry.Status == ShipmentStatus.Delivered),
                    InTransit = group.Count(entry => entry.Status == ShipmentStatus.InTransit)
                });

        var trendStart = DateOnly.FromDateTime(windowStart.Date);
        var trendEnd = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var shipmentVolumeTrend = new List<ShipmentTrendPointDto>();

        for (var date = trendStart; date <= trendEnd; date = date.AddDays(1))
        {
            if (!trendLookup.TryGetValue(date, out var stats))
            {
                stats = new { Total = 0, Delivered = 0, InTransit = 0 };
            }

            shipmentVolumeTrend.Add(new ShipmentTrendPointDto(
                date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                stats.Total,
                stats.Delivered,
                stats.InTransit));
        }

        var warehousePerformance = shipments
            .GroupBy(shipment => new { shipment.WarehouseId, shipment.WarehouseName })
            .Select(group =>
            {
                var deliveredGroup = group
                    .Where(shipment => shipment.Status == ShipmentStatus.Delivered && shipment.DeliveredAt.HasValue)
                    .ToList();

                var deliveredWithEstimates = deliveredGroup
                    .Where(shipment => shipment.EstimatedDeliveryDate.HasValue)
                    .ToList();

                var onTime = deliveredWithEstimates.Count(shipment => shipment.DeliveredAt!.Value <= shipment.EstimatedDeliveryDate!.Value);
                var delayed = deliveredWithEstimates.Count - onTime;

                var warehouseTransitDurations = deliveredGroup
                    .Where(shipment => shipment.ShippedAt.HasValue)
                    .Select(shipment => (shipment.DeliveredAt!.Value - shipment.ShippedAt!.Value).TotalDays)
                    .Where(duration => duration >= 0)
                    .ToList();

                var warehouseAverageTransit = warehouseTransitDurations.Count > 0
                    ? Math.Round(warehouseTransitDurations.Average(), 2)
                    : 0;

                return new WarehousePerformanceDto(
                    group.Key.WarehouseId,
                    string.IsNullOrWhiteSpace(group.Key.WarehouseName) ? "Sin asignar" : group.Key.WarehouseName!,
                    group.Count(),
                    onTime,
                    delayed,
                    warehouseAverageTransit);
            })
            .OrderByDescending(dto => dto.TotalShipments)
            .ToList();

        var carrierPerformance = shipments
            .GroupBy(shipment => new
            {
                shipment.CarrierId,
                Name = string.IsNullOrWhiteSpace(shipment.CarrierName) ? "Sin transportista" : shipment.CarrierName!
            })
            .Select(group =>
            {
                var deliveredGroup = group
                    .Where(shipment => shipment.Status == ShipmentStatus.Delivered && shipment.DeliveredAt.HasValue)
                    .ToList();

                var deliveredWithEstimates = deliveredGroup
                    .Where(shipment => shipment.EstimatedDeliveryDate.HasValue)
                    .ToList();

                var onTime = deliveredWithEstimates.Count(shipment => shipment.DeliveredAt!.Value <= shipment.EstimatedDeliveryDate!.Value);
                var delayDurations = deliveredWithEstimates
                    .Select(shipment => (shipment.DeliveredAt!.Value - shipment.EstimatedDeliveryDate!.Value).TotalDays)
                    .Where(delay => delay > 0)
                    .ToList();

                var onTimeRate = deliveredWithEstimates.Count > 0
                    ? Math.Round(onTime / (double)deliveredWithEstimates.Count, 4)
                    : 0d;

                var averageDelay = delayDurations.Count > 0
                    ? Math.Round(delayDurations.Average(), 2)
                    : 0d;

                return new CarrierPerformanceDto(
                    group.Key.CarrierId,
                    group.Key.Name,
                    group.Count(),
                    group.Count(shipment => shipment.Status == ShipmentStatus.InTransit),
                    deliveredGroup.Count,
                    onTimeRate,
                    averageDelay);
            })
            .OrderByDescending(dto => dto.TotalShipments)
            .ToList();

        var upcomingShipments = shipments
            .Where(shipment => shipment.Status != ShipmentStatus.Delivered)
            .OrderBy(shipment => shipment.EstimatedDeliveryDate ?? shipment.ShippedAt ?? shipment.CreatedAt)
            .Take(5)
            .Select(shipment => new ShipmentSummaryDto(
                shipment.Id,
                shipment.WarehouseId,
                shipment.WarehouseName ?? string.Empty,
                shipment.Status,
                shipment.CreatedAt,
                shipment.ShippedAt,
                shipment.DeliveredAt,
                shipment.CarrierId,
                shipment.CarrierName,
                shipment.TrackingNumber,
                shipment.TotalWeight,
                shipment.EstimatedDeliveryDate))
            .ToList();

        var orderFulfillment = await context.SalesOrders
            .AsNoTracking()
            .Select(order => new
            {
                order.Status,
                TotalOrdered = order.Lines.Sum(line => (double?)line.Quantity) ?? 0d,
                Fulfilled = order.Lines
                    .SelectMany(line => line.Allocations)
                    .Sum(allocation => (double?)allocation.FulfilledQuantity) ?? 0d
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var openSalesOrders = orderFulfillment.Count(order => order.Status is SalesOrderStatus.Pending or SalesOrderStatus.Confirmed or SalesOrderStatus.Shipped);

        var fulfillmentRates = orderFulfillment.Select(order =>
        {
            if (order.TotalOrdered == 0)
            {
                return 0d;
            }

            return order.Fulfilled / order.TotalOrdered;
        }).ToList();

        var averageFulfillmentRate = fulfillmentRates.Count > 0
            ? Math.Round(fulfillmentRates.Average(), 4)
            : 0;

        var replenishmentHandler = new GetReplenishmentPlanQueryHandler(context);
        var replenishmentPlan = await replenishmentHandler.Handle(new GetReplenishmentPlanQuery(null, planningWindowDays), cancellationToken).ConfigureAwait(false);
        var totalReplenishmentRecommendation = replenishmentPlan.Suggestions.Sum(suggestion => suggestion.RecommendedQuantity);

        var dashboard = new LogisticsDashboardDto(
            DateTime.UtcNow,
            totalShipments,
            inTransitShipments,
            deliveredShipments,
            averageTransitDays,
            openSalesOrders,
            averageFulfillmentRate,
            delayedShipments,
            totalReplenishmentRecommendation,
            onTimeDeliveryRate,
            shipmentVolumeTrend,
            warehousePerformance,
            carrierPerformance,
            upcomingShipments);

        var serialized = JsonSerializer.Serialize(dashboard, SerializerOptions);
        await cache.TrySetStringAsync(cacheKey, serialized, CacheOptions, logger, cancellationToken).ConfigureAwait(false);
        await cacheKeyRegistry.TryRegisterKeyAsync(CacheRegions.LogisticsDashboard, cacheKey, logger, cancellationToken).ConfigureAwait(false);

        return dashboard;
    }
}
