using GestorInventario.Application.Analytics.Models;
using GestorInventario.Application.Analytics.Services;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Analytics.Queries;

public record GetDemandForecastQuery(
    int VariantId,
    int Periods,
    decimal? Alpha = null,
    decimal? Beta = null,
    int? SeasonLength = null,
    bool IncludeSeasonality = true) : IRequest<DemandForecastDto>;

public class GetDemandForecastQueryHandler : IRequestHandler<GetDemandForecastQuery, DemandForecastDto>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IDemandForecastService demandForecastService;

    public GetDemandForecastQueryHandler(IGestorInventarioDbContext context, IDemandForecastService demandForecastService)
    {
        this.context = context;
        this.demandForecastService = demandForecastService;
    }

    public async Task<DemandForecastDto> Handle(GetDemandForecastQuery request, CancellationToken cancellationToken)
    {
        var variant = await context.ProductVariants
            .Include(v => v.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VariantId, cancellationToken)
            .ConfigureAwait(false);

        if (variant is null)
        {
            throw new Application.Common.Exceptions.NotFoundException(nameof(ProductVariant), request.VariantId);
        }

        var historical = await context.DemandHistory
            .Where(history => history.VariantId == request.VariantId)
            .OrderBy(history => history.Date)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var historicalPoints = historical
            .Select(history => new DemandPointDto(DateOnly.FromDateTime(history.Date), history.Quantity))
            .ToList();

        var observations = historical
            .Select(history => new DemandObservation(DateOnly.FromDateTime(history.Date), history.Quantity))
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var seasonalFactors = await context.SeasonalFactors
            .Where(factor => factor.VariantId == request.VariantId)
            .Where(factor => factor.Interval == AggregationInterval.Monthly)
            .Where(factor => (!factor.EffectiveFrom.HasValue || factor.EffectiveFrom.Value <= today)
                && (!factor.EffectiveTo.HasValue || factor.EffectiveTo.Value >= today))
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var seasonalDictionary = seasonalFactors.Count > 0
            ? seasonalFactors.ToDictionary(factor => factor.Sequence, factor => factor.Factor)
            : null;

        var parameters = new DemandForecastParameters(
            request.Periods,
            request.Alpha,
            request.Beta,
            request.SeasonLength,
            request.IncludeSeasonality);

        var computation = demandForecastService.GenerateForecast(observations, parameters, seasonalDictionary);

        return new DemandForecastDto(
            variant.Id,
            variant.Sku,
            variant.Product?.Name ?? string.Empty,
            historicalPoints,
            computation.Forecast);
    }
}
