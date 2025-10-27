using GestorInventario.Application.Analytics.Models;
using GestorInventario.Application.Analytics.Services;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Analytics.Queries;

public record GeneratePurchasePlanQuery(
    IReadOnlyCollection<int> VariantIds,
    int Periods = 3,
    decimal? Alpha = null,
    decimal? Beta = null,
    int? SeasonLength = null,
    bool IncludeSeasonality = true,
    decimal ServiceLevel = 0.9m,
    decimal SafetyStockFactor = 1m,
    int? LeadTimeDays = null,
    int? ReviewPeriodDays = null) : IRequest<PurchasePlanDto>;

public class GeneratePurchasePlanQueryHandler : IRequestHandler<GeneratePurchasePlanQuery, PurchasePlanDto>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IDemandForecastService demandForecastService;

    public GeneratePurchasePlanQueryHandler(IGestorInventarioDbContext context, IDemandForecastService demandForecastService)
    {
        this.context = context;
        this.demandForecastService = demandForecastService;
    }

    public async Task<PurchasePlanDto> Handle(GeneratePurchasePlanQuery request, CancellationToken cancellationToken)
    {
        if (request.VariantIds is null || request.VariantIds.Count == 0)
        {
            throw new ArgumentException("Debe especificar al menos una variante para generar el plan de compra.", nameof(request.VariantIds));
        }

        var variantIds = request.VariantIds.Distinct().ToList();

        var variants = await context.ProductVariants
            .Include(variant => variant.Product)
            .Where(variant => variantIds.Contains(variant.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (variants.Count == 0)
        {
            throw new NotFoundException(nameof(ProductVariant), variantIds[0]);
        }

        foreach (var requestedId in variantIds)
        {
            if (variants.All(variant => variant.Id != requestedId))
            {
                throw new NotFoundException(nameof(ProductVariant), requestedId);
            }
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var demandHistory = await context.DemandHistory
            .Where(history => variantIds.Contains(history.VariantId))
            .OrderBy(history => history.Date)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var aggregates = await context.DemandAggregates
            .Where(aggregate => variantIds.Contains(aggregate.VariantId))
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var seasonalFactors = await context.SeasonalFactors
            .Where(factor => variantIds.Contains(factor.VariantId))
            .Where(factor => factor.Interval == AggregationInterval.Monthly)
            .Where(factor => (!factor.EffectiveFrom.HasValue || factor.EffectiveFrom.Value <= today)
                && (!factor.EffectiveTo.HasValue || factor.EffectiveTo.Value >= today))
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var classifications = await context.VariantAbcClassifications
            .Include(classification => classification.Policy)
            .Where(classification => variantIds.Contains(classification.VariantId))
            .Where(classification => classification.EffectiveFrom <= today
                && (!classification.EffectiveTo.HasValue || classification.EffectiveTo.Value >= today))
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var inventoryStocks = await context.InventoryStocks
            .Where(stock => variantIds.Contains(stock.VariantId))
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var parameters = new DemandForecastParameters(
            request.Periods,
            request.Alpha,
            request.Beta,
            request.SeasonLength,
            request.IncludeSeasonality);

        var items = new List<PurchasePlanItemDto>();

        foreach (var variant in variants)
        {
            var variantHistory = demandHistory
                .Where(history => history.VariantId == variant.Id)
                .Select(history => new DemandObservation(DateOnly.FromDateTime(history.Date), history.Quantity))
                .ToList();

            var seasonalityForVariant = seasonalFactors
                .Where(factor => factor.VariantId == variant.Id)
                .ToList();

            IReadOnlyDictionary<int, decimal>? variantSeasonality = seasonalityForVariant.Count > 0
                ? seasonalityForVariant.ToDictionary(factor => factor.Sequence, factor => factor.Factor)
                : null;

            var forecast = demandForecastService.GenerateForecast(variantHistory, parameters, variantSeasonality);
            var forecastedDemand = forecast.Forecast.Sum(point => point.Quantity);

            var variantAggregates = aggregates
                .Where(aggregate => aggregate.VariantId == variant.Id && aggregate.Interval == AggregationInterval.Monthly)
                .OrderByDescending(aggregate => aggregate.PeriodStart)
                .Take(6)
                .ToList();

            decimal averageMonthlyDemand = 0m;
            if (variantAggregates.Count > 0)
            {
                averageMonthlyDemand = variantAggregates.Average(aggregate => aggregate.TotalQuantity);
            }
            else if (variantHistory.Count > 0)
            {
                averageMonthlyDemand = variantHistory
                    .OrderByDescending(history => history.Period)
                    .Take(6)
                    .Average(history => history.Quantity);
            }

            var averageDailyDemand = averageMonthlyDemand > 0m ? averageMonthlyDemand / 30m : (decimal?)null;

            var aggregatedLeadTimes = variantAggregates
                .Where(aggregate => aggregate.AverageLeadTimeDays.HasValue)
                .Select(aggregate => aggregate.AverageLeadTimeDays!.Value)
                .ToList();

            var leadTimeDays = request.LeadTimeDays
                ?? variant.Product?.LeadTimeDays
                ?? (aggregatedLeadTimes.Count > 0 ? (int)Math.Round(aggregatedLeadTimes.Average()) : 14);

            var reviewPeriodDays = request.ReviewPeriodDays ?? 30;

            var classification = classifications.FirstOrDefault(classification => classification.VariantId == variant.Id);
            var serviceLevel = ResolveServiceLevel(classification, request.ServiceLevel);
            var abcClass = classification?.Classification?.ToUpperInvariant();

            decimal safetyStock;
            if (variant.Product?.SafetyStock is { } configuredSafetyStock)
            {
                safetyStock = configuredSafetyStock;
            }
            else if (averageDailyDemand.HasValue)
            {
                var serviceLevelFactor = Math.Max(0.1m, serviceLevel);
                safetyStock = decimal.Round(averageDailyDemand.Value * leadTimeDays * request.SafetyStockFactor * serviceLevelFactor, 2);
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
                reorderPoint = decimal.Round(averageDailyDemand.Value * leadTimeDays + safetyStock, 2);
            }
            else
            {
                reorderPoint = safetyStock;
            }

            var stocks = inventoryStocks.Where(stock => stock.VariantId == variant.Id).ToList();
            var onHand = stocks.Sum(stock => stock.Quantity);
            var reserved = stocks.Sum(stock => stock.ReservedQuantity);
            var available = onHand - reserved;
            var minStockLevel = stocks.Sum(stock => stock.MinStockLevel);

            var recommendedQuantity = decimal.Round(Math.Max(0m, forecastedDemand + reorderPoint - available), 2);

            var unitPrice = variant.Price ?? variant.Product?.DefaultPrice ?? 0m;
            var currency = variant.Product?.Currency ?? string.Empty;

            items.Add(new PurchasePlanItemDto(
                variant.Id,
                variant.Sku,
                variant.Product?.Name ?? string.Empty,
                onHand,
                reserved,
                available,
                minStockLevel,
                decimal.Round(forecastedDemand, 2),
                safetyStock,
                reorderPoint,
                recommendedQuantity,
                averageDailyDemand,
                leadTimeDays,
                reviewPeriodDays,
                serviceLevel,
                abcClass,
                unitPrice,
                currency));
        }

        return new PurchasePlanDto(DateTime.UtcNow, items);
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
