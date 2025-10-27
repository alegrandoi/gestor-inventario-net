using GestorInventario.Application.Analytics.Models;
using GestorInventario.Application.Common.Messaging;

namespace GestorInventario.Application.Analytics.IntegrationEvents;

public sealed record AnalyticsSimulationCompletedIntegrationEvent(
    DateTime GeneratedAt,
    OptimizationScenarioVariantDto Variant,
    OptimizationPolicyDto Policy,
    OptimizationKpiDto Kpis,
    MonteCarloSummaryDto MonteCarlo,
    string Source) : IIntegrationEvent
{
    public string EventName => "analytics.simulation.completed";
}
