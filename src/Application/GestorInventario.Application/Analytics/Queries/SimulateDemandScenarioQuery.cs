using GestorInventario.Application.Analytics.Models;
using GestorInventario.Application.Analytics.Services;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Analytics.Queries;

public record SimulateDemandScenarioQuery(
    int VariantId,
    int Periods = 3,
    IReadOnlyCollection<int>? LeadTimesDays = null,
    decimal? Alpha = null,
    decimal? Beta = null,
    int? SeasonLength = null,
    bool IncludeSeasonality = true,
    decimal SafetyStockFactor = 1m) : IRequest<ScenarioSimulationDto>;

public class SimulateDemandScenarioQueryHandler : IRequestHandler<SimulateDemandScenarioQuery, ScenarioSimulationDto>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IDemandForecastService demandForecastService;

    public SimulateDemandScenarioQueryHandler(IGestorInventarioDbContext context, IDemandForecastService demandForecastService)
    {
        this.context = context;
        this.demandForecastService = demandForecastService;
    }

    public async Task<ScenarioSimulationDto> Handle(SimulateDemandScenarioQuery request, CancellationToken cancellationToken)
    {
        var variant = await context.ProductVariants
            .Include(productVariant => productVariant.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(productVariant => productVariant.Id == request.VariantId, cancellationToken)
            .ConfigureAwait(false);

        if (variant is null)
        {
            throw new NotFoundException(nameof(ProductVariant), request.VariantId);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var history = await context.DemandHistory
            .Where(entry => entry.VariantId == request.VariantId)
            .OrderBy(entry => entry.Date)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var observations = history
            .Select(entry => new DemandObservation(DateOnly.FromDateTime(entry.Date), entry.Quantity))
            .ToList();

        var seasonalityEntries = await context.SeasonalFactors
            .Where(factor => factor.VariantId == request.VariantId)
            .Where(factor => factor.Interval == AggregationInterval.Monthly)
            .Where(factor => (!factor.EffectiveFrom.HasValue || factor.EffectiveFrom.Value <= today)
                && (!factor.EffectiveTo.HasValue || factor.EffectiveTo.Value >= today))
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyDictionary<int, decimal>? seasonalAdjustments = seasonalityEntries.Count > 0
            ? seasonalityEntries.ToDictionary(entry => entry.Sequence, entry => entry.Factor)
            : null;

        var parameters = new DemandForecastParameters(
            request.Periods,
            request.Alpha,
            request.Beta,
            request.SeasonLength,
            request.IncludeSeasonality);

        var forecast = demandForecastService.GenerateForecast(observations, parameters, seasonalAdjustments);
        var forecastedDemand = forecast.Forecast.Sum(point => point.Quantity);

        var aggregates = await context.DemandAggregates
            .Where(aggregate => aggregate.VariantId == request.VariantId && aggregate.Interval == AggregationInterval.Monthly)
            .OrderByDescending(aggregate => aggregate.PeriodStart)
            .Take(6)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        decimal averageMonthlyDemand = 0m;
        if (aggregates.Count > 0)
        {
            averageMonthlyDemand = aggregates.Average(aggregate => aggregate.TotalQuantity);
        }
        else if (observations.Count > 0)
        {
            averageMonthlyDemand = observations
                .OrderByDescending(observation => observation.Period)
                .Take(6)
                .Average(observation => observation.Quantity);
        }

        var averageDailyDemand = averageMonthlyDemand > 0m ? averageMonthlyDemand / 30m : (decimal?)null;

        var aggregatedLeadTimes = aggregates
            .Where(aggregate => aggregate.AverageLeadTimeDays.HasValue)
            .Select(aggregate => aggregate.AverageLeadTimeDays!.Value)
            .ToList();

        var baselineLeadTime = variant.Product?.LeadTimeDays
            ?? (aggregatedLeadTimes.Count > 0 ? (int)Math.Round(aggregatedLeadTimes.Average()) : 14);

        var stocks = await context.InventoryStocks
            .Where(stock => stock.VariantId == request.VariantId)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var onHand = stocks.Sum(stock => stock.Quantity);
        var reserved = stocks.Sum(stock => stock.ReservedQuantity);
        var available = onHand - reserved;

        var classification = await context.VariantAbcClassifications
            .Include(entry => entry.Policy)
            .Where(entry => entry.VariantId == request.VariantId)
            .Where(entry => entry.EffectiveFrom <= today
                && (!entry.EffectiveTo.HasValue || entry.EffectiveTo.Value >= today))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var serviceLevel = ResolveServiceLevel(classification, 0.9m);
        var abcClass = classification?.Classification?.ToUpperInvariant();

        var leadTimeOptions = (request.LeadTimesDays?.Count > 0
                ? request.LeadTimesDays
                : new[]
                {
                    baselineLeadTime,
                    Math.Max(1, baselineLeadTime + 7),
                    Math.Max(1, baselineLeadTime - 7)
                })
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToList();

        var scenarios = new List<ScenarioSimulationResultDto>();

        foreach (var leadTime in leadTimeOptions)
        {
            decimal safetyStock;
            if (variant.Product?.SafetyStock is { } configuredSafetyStock)
            {
                safetyStock = configuredSafetyStock;
            }
            else if (averageDailyDemand.HasValue)
            {
                safetyStock = decimal.Round(
                    averageDailyDemand.Value * leadTime * request.SafetyStockFactor * Math.Max(0.1m, serviceLevel),
                    2);
            }
            else
            {
                safetyStock = 0m;
            }

            decimal reorderPoint;
            if (variant.Product?.ReorderPoint is { } configuredReorderPoint)
            {
                reorderPoint = configuredReorderPoint;
            }
            else if (averageDailyDemand.HasValue)
            {
                reorderPoint = decimal.Round(averageDailyDemand.Value * leadTime + safetyStock, 2);
            }
            else
            {
                reorderPoint = safetyStock;
            }

            var coverage = decimal.Round(forecastedDemand, 2);
            var denominator = coverage + safetyStock;
            var stockoutRisk = denominator <= 0m
                ? 0m
                : Math.Max(0m, denominator - available) / denominator;

            var recommendedOrder = decimal.Round(Math.Max(0m, coverage + reorderPoint - available), 2);

            var residualRisk = denominator <= 0m
                ? 0m
                : Math.Max(0m, denominator - (available + recommendedOrder)) / denominator;

            scenarios.Add(new ScenarioSimulationResultDto(
                leadTime,
                coverage,
                safetyStock,
                reorderPoint,
                decimal.Round(stockoutRisk, 4),
                decimal.Round(residualRisk, 4),
                recommendedOrder));
        }

        var variantScenario = new ScenarioSimulationVariantDto(
            variant.Id,
            variant.Sku,
            variant.Product?.Name ?? string.Empty,
            onHand,
            reserved,
            decimal.Round(forecastedDemand, 2),
            serviceLevel,
            abcClass,
            scenarios);

        return new ScenarioSimulationDto(DateTime.UtcNow, new[] { variantScenario });
    }

    private static decimal ResolveServiceLevel(VariantAbcClassification? classification, decimal fallback)
    {
        if (classification?.Policy is null)
        {
            return fallback;
        }

        return classification.Classification?.ToUpperInvariant() switch
        {
            "A" => classification.Policy.ServiceLevelA,
            "B" => classification.Policy.ServiceLevelB,
            "C" => classification.Policy.ServiceLevelC,
            _ => fallback
        };
    }
}
