using System.Linq;

namespace GestorInventario.Application.Analytics.Optimization;

public interface IInventoryOptimizationService
{
    OptimizationRecommendationResult GenerateRecommendations(IEnumerable<OptimizationInput> inputs, MonteCarloOptions options);

    OptimizationScenarioComparisonResult CompareScenarios(
        OptimizationInput baseline,
        IEnumerable<OptimizationScenarioAdjustment> adjustments,
        MonteCarloOptions options);
}

public sealed record OptimizationInput(
    int VariantId,
    string VariantSku,
    string ProductName,
    decimal OnHand,
    decimal Reserved,
    decimal Available,
    decimal AverageDailyDemand,
    decimal DemandStandardDeviation,
    int LeadTimeDays,
    int ReviewPeriodDays,
    decimal UnitPrice,
    string Currency,
    decimal HoldingCostRate,
    decimal OrderingCost,
    decimal ServiceLevel,
    decimal StockoutCost,
    decimal MinStockLevel,
    decimal? ConfiguredReorderPoint,
    decimal? ConfiguredReorderQuantity);

public sealed record OptimizationScenarioAdjustment(
    string Name,
    decimal? ServiceLevel = null,
    int? LeadTimeDays = null,
    int? ReviewPeriodDays = null,
    decimal? HoldingCostRate = null,
    decimal? OrderingCost = null,
    decimal? StockoutCost = null);

public sealed record MonteCarloOptions(int Iterations, int Seed);

public sealed record OptimizationRecommendationResult(
    DateTime GeneratedAt,
    IReadOnlyCollection<OptimizationPolicy> Policies,
    IReadOnlyDictionary<int, MonteCarloSummary> MonteCarloSummaries,
    IReadOnlyDictionary<int, OptimizationKpi> Kpis);

public sealed record OptimizationScenarioComparisonResult(
    DateTime GeneratedAt,
    OptimizationScenarioVariant Variant,
    OptimizationScenarioOutcome Baseline,
    IReadOnlyCollection<OptimizationScenarioOutcome> Alternatives);

public sealed record OptimizationScenarioVariant(int VariantId, string VariantSku, string ProductName);

public sealed record OptimizationScenarioOutcome(
    string ScenarioName,
    OptimizationPolicy Policy,
    OptimizationKpi Kpis,
    MonteCarloSummary MonteCarlo);

public sealed record OptimizationPolicy(
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
    string Currency);

public sealed record OptimizationKpi(
    decimal FillRate,
    decimal TotalCost,
    decimal HoldingCost,
    decimal OrderingCost,
    decimal StockoutRisk,
    decimal AverageInventory);

public sealed record MonteCarloSummary(
    int Iterations,
    decimal AverageFillRate,
    decimal AverageTotalCost,
    decimal StockoutProbability);

public class InventoryOptimizationService : IInventoryOptimizationService
{
    private readonly Random random;

    public InventoryOptimizationService()
    {
        random = Random.Shared;
    }

    public OptimizationRecommendationResult GenerateRecommendations(IEnumerable<OptimizationInput> inputs, MonteCarloOptions options)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var inputList = inputs.ToList();
        var policies = new List<OptimizationPolicy>(inputList.Count);
        var kpis = new Dictionary<int, OptimizationKpi>();
        var simulations = new Dictionary<int, MonteCarloSummary>();

        foreach (var input in inputList)
        {
            var policy = CalculatePolicy(input);
            policies.Add(policy);
            kpis[input.VariantId] = CalculateKpis(input, policy);
            simulations[input.VariantId] = RunMonteCarloSimulation(input, policy, options);
        }

        return new OptimizationRecommendationResult(DateTime.UtcNow, policies, simulations, kpis);
    }

    public OptimizationScenarioComparisonResult CompareScenarios(
        OptimizationInput baseline,
        IEnumerable<OptimizationScenarioAdjustment> adjustments,
        MonteCarloOptions options)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(adjustments);

        var baselinePolicy = CalculatePolicy(baseline);
        var baselineKpi = CalculateKpis(baseline, baselinePolicy);
        var baselineSimulation = RunMonteCarloSimulation(baseline, baselinePolicy, options);

        var outcomes = new List<OptimizationScenarioOutcome>();

        foreach (var adjustment in adjustments)
        {
            var scenarioInput = ApplyAdjustment(baseline, adjustment);
            var scenarioPolicy = CalculatePolicy(scenarioInput);
            var scenarioKpis = CalculateKpis(scenarioInput, scenarioPolicy);
            var scenarioSimulation = RunMonteCarloSimulation(scenarioInput, scenarioPolicy, options);
            outcomes.Add(new OptimizationScenarioOutcome(adjustment.Name, scenarioPolicy, scenarioKpis, scenarioSimulation));
        }

        return new OptimizationScenarioComparisonResult(
            DateTime.UtcNow,
            new OptimizationScenarioVariant(baseline.VariantId, baseline.VariantSku, baseline.ProductName),
            new OptimizationScenarioOutcome("Base", baselinePolicy, baselineKpi, baselineSimulation),
            outcomes);
    }

    private OptimizationPolicy CalculatePolicy(OptimizationInput input)
    {
        var dailyDemand = Math.Max(0m, input.AverageDailyDemand);
        var leadTimeDays = Math.Max(1, input.LeadTimeDays);
        var reviewPeriodDays = Math.Max(1, input.ReviewPeriodDays);
        var serviceLevel = Clamp(input.ServiceLevel, 0.5m, 0.999m);
        var zScore = ResolveZScore(serviceLevel);
        var demandDeviation = Math.Max(0.01m, input.DemandStandardDeviation);
        var safetyStock = decimal.Round(zScore * demandDeviation * (decimal)Math.Sqrt(leadTimeDays), 2, MidpointRounding.AwayFromZero);

        decimal reorderPoint;
        if (input.ConfiguredReorderPoint.HasValue)
        {
            reorderPoint = input.ConfiguredReorderPoint.Value;
        }
        else
        {
            reorderPoint = decimal.Round(dailyDemand * leadTimeDays + safetyStock, 2, MidpointRounding.AwayFromZero);
        }

        var cycleStock = decimal.Round(dailyDemand * reviewPeriodDays, 2, MidpointRounding.AwayFromZero);
        var minStock = decimal.Round(Math.Max(0m, input.MinStockLevel > 0 ? input.MinStockLevel : reorderPoint - cycleStock / 2m), 2, MidpointRounding.AwayFromZero);
        var maxStock = decimal.Round(reorderPoint + cycleStock, 2, MidpointRounding.AwayFromZero);

        var holdingCostRate = Math.Max(0.01m, input.HoldingCostRate);
        var annualDemand = dailyDemand * 365m;
        var holdingCostPerUnit = input.UnitPrice * holdingCostRate;
        var orderingCost = Math.Max(0m, input.OrderingCost);

        decimal eoq = 0m;
        if (holdingCostPerUnit > 0m && annualDemand > 0m && orderingCost > 0m)
        {
            eoq = decimal.Round((decimal)Math.Sqrt((double)(2m * annualDemand * orderingCost) / (double)holdingCostPerUnit), 2, MidpointRounding.AwayFromZero);
        }
        else if (input.ConfiguredReorderQuantity.HasValue)
        {
            eoq = input.ConfiguredReorderQuantity.Value;
        }

        return new OptimizationPolicy(
            input.VariantId,
            input.VariantSku,
            input.ProductName,
            input.OnHand,
            input.Reserved,
            input.Available,
            minStock,
            maxStock,
            safetyStock,
            reorderPoint,
            eoq,
            serviceLevel,
            dailyDemand,
            reviewPeriodDays,
            holdingCostRate,
            orderingCost,
            input.UnitPrice,
            input.Currency);
    }

    private OptimizationKpi CalculateKpis(OptimizationInput input, OptimizationPolicy policy)
    {
        var reviewDemand = policy.AverageDailyDemand * policy.ReviewPeriodDays;
        var available = input.Available + policy.EconomicOrderQuantity;
        var shortage = Math.Max(0m, reviewDemand - available);
        var fillRate = reviewDemand <= 0m ? 1m : Math.Max(0m, 1m - shortage / reviewDemand);

        var averageInventory = (policy.MinStockLevel + policy.MaxStockLevel) / 2m;
        var holdingCost = decimal.Round(averageInventory * input.UnitPrice * policy.HoldingCostRate / 365m * policy.ReviewPeriodDays, 2, MidpointRounding.AwayFromZero);
        var orderingCost = decimal.Round(policy.OrderingCost, 2, MidpointRounding.AwayFromZero);
        var stockoutCost = decimal.Round(shortage * input.StockoutCost, 2, MidpointRounding.AwayFromZero);
        var totalCost = decimal.Round(holdingCost + orderingCost + stockoutCost, 2, MidpointRounding.AwayFromZero);
        var stockoutRisk = reviewDemand <= 0m ? 0m : Math.Min(1m, shortage / (reviewDemand + policy.SafetyStock));

        return new OptimizationKpi(
            decimal.Round(fillRate, 4, MidpointRounding.AwayFromZero),
            totalCost,
            holdingCost,
            orderingCost,
            decimal.Round(stockoutRisk, 4, MidpointRounding.AwayFromZero),
            decimal.Round(averageInventory, 2, MidpointRounding.AwayFromZero));
    }

    private MonteCarloSummary RunMonteCarloSimulation(OptimizationInput input, OptimizationPolicy policy, MonteCarloOptions options)
    {
        var iterations = Math.Max(10, options.Iterations);
        var fillRates = new decimal[iterations];
        var totalCosts = new decimal[iterations];
        var stockouts = 0;

        var randomInstance = new Random(options.Seed == 0 ? random.Next() : options.Seed);

        for (var index = 0; index < iterations; index++)
        {
            var simulatedDemand = SampleNormal(
                randomInstance,
                policy.AverageDailyDemand * policy.ReviewPeriodDays,
                input.DemandStandardDeviation * (decimal)Math.Sqrt(policy.ReviewPeriodDays));

            var available = input.Available + policy.EconomicOrderQuantity;
            var shortage = Math.Max(0m, simulatedDemand - available);

            var fillRate = simulatedDemand <= 0m ? 1m : Math.Max(0m, 1m - shortage / simulatedDemand);
            if (shortage > 0m)
            {
                stockouts++;
            }

            var averageInventory = (policy.MinStockLevel + policy.MaxStockLevel) / 2m;
            var holdingCost = averageInventory * input.UnitPrice * policy.HoldingCostRate / 365m * policy.ReviewPeriodDays;
            var orderingCost = policy.OrderingCost;
            var stockoutCost = shortage * input.StockoutCost;
            var totalCost = holdingCost + orderingCost + stockoutCost;

            fillRates[index] = fillRate;
            totalCosts[index] = totalCost;
        }

        var averageFillRate = fillRates.Length == 0 ? 0m : decimal.Round(fillRates.Average(), 4, MidpointRounding.AwayFromZero);
        var averageCost = totalCosts.Length == 0 ? 0m : decimal.Round(totalCosts.Average(), 2, MidpointRounding.AwayFromZero);
        var stockoutProbability = iterations == 0 ? 0m : decimal.Round((decimal)stockouts / iterations, 4, MidpointRounding.AwayFromZero);

        return new MonteCarloSummary(iterations, averageFillRate, averageCost, stockoutProbability);
    }

    private static OptimizationInput ApplyAdjustment(OptimizationInput baseline, OptimizationScenarioAdjustment adjustment)
    {
        return baseline with
        {
            ServiceLevel = adjustment.ServiceLevel ?? baseline.ServiceLevel,
            LeadTimeDays = adjustment.LeadTimeDays ?? baseline.LeadTimeDays,
            ReviewPeriodDays = adjustment.ReviewPeriodDays ?? baseline.ReviewPeriodDays,
            HoldingCostRate = adjustment.HoldingCostRate ?? baseline.HoldingCostRate,
            OrderingCost = adjustment.OrderingCost ?? baseline.OrderingCost,
            StockoutCost = adjustment.StockoutCost ?? baseline.StockoutCost
        };
    }

    private static decimal Clamp(decimal value, decimal minimum, decimal maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private static decimal ResolveZScore(decimal serviceLevel)
    {
        if (serviceLevel <= 0.5m)
        {
            return 0m;
        }

        return serviceLevel switch
        {
            <= 0.80m => 0.84m,
            <= 0.85m => 1.04m,
            <= 0.90m => 1.28m,
            <= 0.95m => 1.64m,
            <= 0.98m => 2.05m,
            <= 0.99m => 2.33m,
            _ => 2.6m
        };
    }

    private static decimal SampleNormal(Random random, decimal mean, decimal standardDeviation)
    {
        if (standardDeviation <= 0m)
        {
            return mean;
        }

        var u1 = 1.0 - random.NextDouble();
        var u2 = 1.0 - random.NextDouble();
        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        var value = mean + standardDeviation * (decimal)randStdNormal;
        return value < 0m ? 0m : value;
    }
}
