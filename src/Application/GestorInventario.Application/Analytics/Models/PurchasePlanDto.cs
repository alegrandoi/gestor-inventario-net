namespace GestorInventario.Application.Analytics.Models;

public record PurchasePlanDto(
    DateTime GeneratedAt,
    IReadOnlyCollection<PurchasePlanItemDto> Items);

public record PurchasePlanItemDto(
    int VariantId,
    string VariantSku,
    string ProductName,
    decimal OnHand,
    decimal Reserved,
    decimal Available,
    decimal MinStockLevel,
    decimal ForecastedDemand,
    decimal SafetyStock,
    decimal ReorderPoint,
    decimal RecommendedOrderQuantity,
    decimal? AverageDailyDemand,
    int LeadTimeDays,
    int ReviewPeriodDays,
    decimal? ServiceLevel,
    string? AbcClass,
    decimal UnitPrice,
    string Currency);
