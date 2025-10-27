using GestorInventario.Application.Analytics.Models;

namespace GestorInventario.Application.Analytics.Services;

public interface IDemandForecastService
{
    DemandForecastResult GenerateForecast(
        IReadOnlyList<DemandObservation> history,
        DemandForecastParameters parameters,
        IReadOnlyDictionary<int, decimal>? seasonalAdjustments = null);
}

public sealed record DemandObservation(DateOnly Period, decimal Quantity);

public sealed record DemandForecastParameters(
    int Periods,
    decimal? Alpha,
    decimal? Beta,
    int? SeasonLength,
    bool IncludeSeasonality);

public sealed record DemandForecastResult(
    IReadOnlyCollection<DemandPointDto> SmoothedHistory,
    IReadOnlyCollection<DemandPointDto> Forecast,
    decimal LastLevel,
    decimal? LastTrend,
    DemandForecastParameters Parameters);

public class DemandForecastService : IDemandForecastService
{
    private const decimal DefaultAlpha = 0.3m;
    private const decimal DefaultBeta = 0.1m;

    public DemandForecastResult GenerateForecast(
        IReadOnlyList<DemandObservation> history,
        DemandForecastParameters parameters,
        IReadOnlyDictionary<int, decimal>? seasonalAdjustments = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(parameters);

        var orderedHistory = history
            .OrderBy(observation => observation.Period)
            .ToList();

        var alpha = Clamp(parameters.Alpha ?? DefaultAlpha, 0.01m, 0.99m);
        var useTrend = parameters.Beta.HasValue && parameters.Beta.Value > 0m;
        var beta = useTrend
            ? Clamp(parameters.Beta!.Value, 0.01m, 0.99m)
            : Clamp(DefaultBeta, 0.01m, 0.99m);

        if (orderedHistory.Count == 0)
        {
            var emptyHistory = Array.Empty<DemandPointDto>();
            var emptyForecast = GenerateNeutralForecast(parameters.Periods);
            return new DemandForecastResult(emptyHistory, emptyForecast, 0m, useTrend ? 0m : null, parameters);
        }

        decimal level = orderedHistory[0].Quantity;
        decimal trend = 0m;

        if (useTrend && orderedHistory.Count > 1)
        {
            trend = orderedHistory[1].Quantity - orderedHistory[0].Quantity;
        }

        var smoothed = new List<DemandPointDto>(orderedHistory.Count);

        foreach (var observation in orderedHistory)
        {
            var previousLevel = level;
            level = alpha * observation.Quantity + (1 - alpha) * (level + (useTrend ? trend : 0m));

            if (useTrend)
            {
                trend = beta * (level - previousLevel) + (1 - beta) * trend;
            }

            smoothed.Add(new DemandPointDto(observation.Period, decimal.Round(level, 2)));
        }

        var lastPeriod = orderedHistory[^1].Period;
        var forecast = new List<DemandPointDto>(parameters.Periods);

        IReadOnlyDictionary<int, decimal>? normalizedSeasonality = null;
        if (parameters.IncludeSeasonality && seasonalAdjustments is { Count: > 0 })
        {
            normalizedSeasonality = seasonalAdjustments;
        }

        var seasonLength = parameters.SeasonLength ?? normalizedSeasonality?.Count;

        for (var step = 1; step <= parameters.Periods; step++)
        {
            var nextPeriod = lastPeriod.AddMonths(step);
            var projection = level + (useTrend ? trend * step : 0m);

            if (normalizedSeasonality is not null)
            {
                if (seasonLength.HasValue && seasonLength.Value > 0)
                {
                    var sequence = ResolveSequence(lastPeriod, step, seasonLength.Value);
                    if (normalizedSeasonality.TryGetValue(sequence, out var factor))
                    {
                        projection *= factor;
                    }
                }
                else if (normalizedSeasonality.TryGetValue(nextPeriod.Month, out var factor))
                {
                    projection *= factor;
                }
            }

            forecast.Add(new DemandPointDto(nextPeriod, decimal.Round(Math.Max(0m, projection), 2)));
        }

        return new DemandForecastResult(
            smoothed,
            forecast,
            decimal.Round(level, 4),
            useTrend ? decimal.Round(trend, 4) : null,
            parameters);
    }

    private static IReadOnlyCollection<DemandPointDto> GenerateNeutralForecast(int periods)
    {
        var forecast = new List<DemandPointDto>(Math.Max(0, periods));
        var start = DateOnly.FromDateTime(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1));

        for (var index = 1; index <= periods; index++)
        {
            forecast.Add(new DemandPointDto(start.AddMonths(index), 0m));
        }

        return forecast;
    }

    private static decimal Clamp(decimal value, decimal minimum, decimal maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private static int ResolveSequence(DateOnly basePeriod, int step, int seasonLength)
    {
        var nextPeriod = basePeriod.AddMonths(step);
        return ((nextPeriod.Month - 1) % seasonLength) + 1;
    }
}
