using System;
using System.Collections.Generic;
using System.Linq;
using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.Analytics.Queries;

internal static class OptimizationQueryHelper
{
    public static decimal CalculateStandardDeviation(IReadOnlyCollection<decimal> values)
    {
        if (values is null || values.Count < 2)
        {
            return 0m;
        }

        var mean = values.Average();
        var variance = values.Sum(value => (value - mean) * (value - mean)) / (values.Count - 1);
        return (decimal)Math.Sqrt((double)variance);
    }

    public static decimal ResolveServiceLevel(VariantAbcClassification? classification, decimal fallback)
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
