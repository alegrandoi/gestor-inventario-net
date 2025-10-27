using System;
using System.Collections.Generic;
using System.Linq;
using GestorInventario.Application.Analytics.Models;
using GestorInventario.Application.Analytics.Services;
using GestorInventario.Application.Analytics.Optimization;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Analytics.Queries;

public record GenerateOptimizationRecommendationsQuery(
    IReadOnlyCollection<int> VariantIds,
    int Periods = 3,
    decimal? Alpha = null,
    decimal? Beta = null,
    int? SeasonLength = null,
    bool IncludeSeasonality = true,
    int? LeadTimeDays = null,
    int? ReviewPeriodDays = null,
    decimal? ServiceLevel = null,
    decimal? HoldingCostRate = null,
    decimal? OrderingCost = null,
    decimal? StockoutCost = null,
    int MonteCarloIterations = 500) : IRequest<OptimizationRecommendationDto>;

public class GenerateOptimizationRecommendationsQueryHandler : IRequestHandler<GenerateOptimizationRecommendationsQuery, OptimizationRecommendationDto>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IDemandForecastService demandForecastService;
    private readonly IInventoryOptimizationService optimizationService;

    public GenerateOptimizationRecommendationsQueryHandler(
        IGestorInventarioDbContext context,
        IDemandForecastService demandForecastService,
        IInventoryOptimizationService optimizationService)
    {
        this.context = context;
        this.demandForecastService = demandForecastService;
        this.optimizationService = optimizationService;
    }

    public async Task<OptimizationRecommendationDto> Handle(GenerateOptimizationRecommendationsQuery request, CancellationToken cancellationToken)
    {
        if (request.VariantIds is null || request.VariantIds.Count == 0)
        {
            throw new ValidationException("Debe indicar al menos una variante para generar las recomendaciones.");
        }

        var variants = await context.ProductVariants
            .Include(variant => variant.Product)
            .Where(variant => request.VariantIds.Contains(variant.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (variants.Count == 0)
        {
            throw new NotFoundException(nameof(ProductVariant), string.Join(",", request.VariantIds));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var inputs = new List<OptimizationInput>(variants.Count);

        foreach (var variant in variants)
        {
            var history = await context.DemandHistory
                .Where(entry => entry.VariantId == variant.Id)
                .OrderBy(entry => entry.Date)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var observations = history
                .Select(entry => new DemandObservation(DateOnly.FromDateTime(entry.Date), entry.Quantity))
                .ToList();

            IReadOnlyDictionary<int, decimal>? seasonalAdjustments = null;
            if (request.IncludeSeasonality)
            {
                var seasonalFactors = await context.SeasonalFactors
                    .Where(factor => factor.VariantId == variant.Id)
                    .Where(factor => factor.Interval == AggregationInterval.Monthly)
                    .Where(factor => (!factor.EffectiveFrom.HasValue || factor.EffectiveFrom.Value <= today)
                        && (!factor.EffectiveTo.HasValue || factor.EffectiveTo.Value >= today))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (seasonalFactors.Count > 0)
                {
                    seasonalAdjustments = seasonalFactors.ToDictionary(factor => factor.Sequence, factor => factor.Factor);
                }
            }

            var parameters = new DemandForecastParameters(
                request.Periods,
                request.Alpha,
                request.Beta,
                request.SeasonLength,
                request.IncludeSeasonality);

            var forecast = demandForecastService.GenerateForecast(observations, parameters, seasonalAdjustments);

            var monthlyHistory = history
                .GroupBy(entry => new { entry.Date.Year, entry.Date.Month })
                .Select(group => group.Sum(entry => entry.Quantity))
                .OrderBy(quantity => quantity)
                .ToList();

            var averageMonthlyDemand = monthlyHistory.Count > 0
                ? monthlyHistory.Average()
                : forecast.Forecast.Sum(point => point.Quantity) / Math.Max(1, request.Periods);

            var averageDailyDemand = averageMonthlyDemand / 30m;
            var demandStdDev = OptimizationQueryHelper.CalculateStandardDeviation(monthlyHistory) / 30m;

            var aggregates = await context.DemandAggregates
                .Where(aggregate => aggregate.VariantId == variant.Id && aggregate.Interval == AggregationInterval.Monthly)
                .OrderByDescending(aggregate => aggregate.PeriodStart)
                .Take(6)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var leadTimes = aggregates
                .Where(aggregate => aggregate.AverageLeadTimeDays.HasValue)
                .Select(aggregate => aggregate.AverageLeadTimeDays!.Value)
                .ToList();

            var leadTimeDays = request.LeadTimeDays
                ?? variant.Product?.LeadTimeDays
                ?? (leadTimes.Count > 0 ? (int)Math.Round(leadTimes.Average()) : 14);

            var reviewPeriodDays = request.ReviewPeriodDays ?? 30;

            var classification = await context.VariantAbcClassifications
                .Include(entry => entry.Policy)
                .Where(entry => entry.VariantId == variant.Id)
                .Where(entry => entry.EffectiveFrom <= today && (!entry.EffectiveTo.HasValue || entry.EffectiveTo.Value >= today))
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var serviceLevel = request.ServiceLevel ?? OptimizationQueryHelper.ResolveServiceLevel(classification, 0.92m);
            var holdingCostRate = request.HoldingCostRate ?? 0.2m;
            var orderingCost = request.OrderingCost ?? 25m;
            var stockoutCost = request.StockoutCost ?? Math.Max(5m, (variant.Price ?? variant.Product?.DefaultPrice ?? 10m) * 1.5m);

            var stocks = await context.InventoryStocks
                .Where(stock => stock.VariantId == variant.Id)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var onHand = stocks.Sum(stock => stock.Quantity);
            var reserved = stocks.Sum(stock => stock.ReservedQuantity);
            var available = onHand - reserved;
            var minStockLevel = stocks.Count > 0 ? stocks.Average(stock => stock.MinStockLevel) : 0m;

            inputs.Add(new OptimizationInput(
                variant.Id,
                variant.Sku,
                variant.Product?.Name ?? string.Empty,
                onHand,
                reserved,
                available,
                decimal.Round(averageDailyDemand, 4, MidpointRounding.AwayFromZero),
                decimal.Round(Math.Max(0.01m, demandStdDev), 4, MidpointRounding.AwayFromZero),
                leadTimeDays,
                reviewPeriodDays,
                variant.Price ?? variant.Product?.DefaultPrice ?? 0m,
                variant.Product?.Currency ?? "EUR",
                holdingCostRate,
                orderingCost,
                serviceLevel,
                stockoutCost,
                decimal.Round(minStockLevel, 2, MidpointRounding.AwayFromZero),
                variant.Product?.ReorderPoint,
                variant.Product?.ReorderQuantity));
        }

        var seed = Environment.TickCount;
        var recommendationResult = optimizationService.GenerateRecommendations(
            inputs,
            new MonteCarloOptions(Math.Max(10, request.MonteCarloIterations), seed));

        var dtoPolicies = recommendationResult.Policies
            .Select(policy =>
            {
                var kpi = recommendationResult.Kpis[policy.VariantId];
                var simulation = recommendationResult.MonteCarloSummaries[policy.VariantId];
                return new OptimizationPolicyDto(
                    policy.VariantId,
                    policy.VariantSku,
                    policy.ProductName,
                    policy.OnHand,
                    policy.Reserved,
                    policy.Available,
                    policy.MinStockLevel,
                    policy.MaxStockLevel,
                    policy.SafetyStock,
                    policy.ReorderPoint,
                    policy.EconomicOrderQuantity,
                    policy.TargetServiceLevel,
                    policy.AverageDailyDemand,
                    policy.ReviewPeriodDays,
                    policy.HoldingCostRate,
                    policy.OrderingCost,
                    policy.UnitPrice,
                    policy.Currency,
                    new OptimizationKpiDto(
                        kpi.FillRate,
                        kpi.TotalCost,
                        kpi.HoldingCost,
                        kpi.OrderingCost,
                        kpi.StockoutRisk,
                        kpi.AverageInventory),
                    new MonteCarloSummaryDto(
                        simulation.Iterations,
                        simulation.AverageFillRate,
                        simulation.AverageTotalCost,
                        simulation.StockoutProbability));
            })
            .OrderBy(policy => policy.VariantSku)
            .ToList();

        return new OptimizationRecommendationDto(DateTime.UtcNow, dtoPolicies);
    }

}
