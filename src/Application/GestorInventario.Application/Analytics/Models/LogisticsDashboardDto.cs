using GestorInventario.Application.SalesOrders.Models;

namespace GestorInventario.Application.Analytics.Models;

public record LogisticsDashboardDto(
    DateTime GeneratedAt,
    int TotalShipments,
    int InTransitShipments,
    int DeliveredShipments,
    double AverageTransitDays,
    int OpenSalesOrders,
    double AverageFulfillmentRate,
    IReadOnlyCollection<ShipmentSummaryDto> TopDelayedShipments,
    decimal TotalReplenishmentRecommendation,
    double OnTimeDeliveryRate,
    IReadOnlyCollection<ShipmentTrendPointDto> ShipmentVolumeTrend,
    IReadOnlyCollection<WarehousePerformanceDto> WarehousePerformance,
    IReadOnlyCollection<CarrierPerformanceDto> CarrierPerformance,
    IReadOnlyCollection<ShipmentSummaryDto> UpcomingShipments);
