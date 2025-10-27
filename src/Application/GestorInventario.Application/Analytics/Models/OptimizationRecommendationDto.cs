using GestorInventario.Application.Analytics.Optimization;

namespace GestorInventario.Application.Analytics.Models;

public record OptimizationRecommendationDto(
    DateTime GeneratedAt,
    IReadOnlyCollection<OptimizationPolicyDto> Policies);

public record OptimizationPolicyDto(
    int VariantId,
    string VariantSku,
    string ProductName,
    decimal OnHand,
    decimal Reserved,
    decimal Available,
    decimal MinStockLevel,
    decimal MaxStockLevel,
    decimal SafetyStock,
    decimal ReorderPoint,
    decimal EconomicOrderQuantity,
    decimal TargetServiceLevel,
    decimal AverageDailyDemand,
    int ReviewPeriodDays,
    decimal HoldingCostRate,
    decimal OrderingCost,
    decimal UnitPrice,
    string Currency,
    OptimizationKpiDto Kpis,
    MonteCarloSummaryDto MonteCarlo);

public record OptimizationScenarioComparisonDto(
    DateTime GeneratedAt,
    OptimizationScenarioVariantDto Variant,
    OptimizationScenarioOutcomeDto Baseline,
    IReadOnlyCollection<OptimizationScenarioOutcomeDto> Alternatives);

public record OptimizationScenarioVariantDto(int VariantId, string VariantSku, string ProductName);

public record OptimizationScenarioOutcomeDto(
    string ScenarioName,
    OptimizationPolicyDto Policy,
    OptimizationKpiDto Kpis,
    MonteCarloSummaryDto MonteCarlo);

public record OptimizationKpiDto(
    decimal FillRate,
    decimal TotalCost,
    decimal HoldingCost,
    decimal OrderingCost,
    decimal StockoutRisk,
    decimal AverageInventory);

public record MonteCarloSummaryDto(
    int Iterations,
    decimal AverageFillRate,
    decimal AverageTotalCost,
    decimal StockoutProbability);
