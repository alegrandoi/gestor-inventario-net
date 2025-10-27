namespace GestorInventario.Application.Analytics.Models;

public record ScenarioSimulationDto(
    DateTime GeneratedAt,
    IReadOnlyCollection<ScenarioSimulationVariantDto> Variants);

public record ScenarioSimulationVariantDto(
    int VariantId,
    string VariantSku,
    string ProductName,
    decimal OnHand,
    decimal Reserved,
    decimal ForecastedDemand,
    decimal? ServiceLevel,
    string? AbcClass,
    IReadOnlyCollection<ScenarioSimulationResultDto> Scenarios);

public record ScenarioSimulationResultDto(
    int LeadTimeDays,
    decimal ForecastedDemand,
    decimal SafetyStock,
    decimal ReorderPoint,
    decimal StockoutRisk,
    decimal ResidualRisk,
    decimal RecommendedOrderQuantity);
