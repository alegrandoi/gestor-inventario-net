using GestorInventario.Application.Analytics.Models;
using GestorInventario.Application.Analytics.Queries;
using GestorInventario.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner},{RoleNames.InventoryManager}")]
public class AnalyticsController : ApiControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(InventoryDashboardDto), StatusCodes.Status200OK)]
    public async Task<InventoryDashboardDto> GetDashboard(CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetInventoryDashboardQuery(), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("demand/{variantId:int}")]
    [ProducesResponseType(typeof(DemandForecastDto), StatusCodes.Status200OK)]
    public async Task<DemandForecastDto> GetDemandForecast(
        int variantId,
        [FromQuery] int periods = 3,
        [FromQuery] decimal? alpha = null,
        [FromQuery] decimal? beta = null,
        [FromQuery] int? seasonLength = null,
        [FromQuery] bool includeSeasonality = true,
        CancellationToken cancellationToken = default)
    {
        return await Sender.Send(
                new GetDemandForecastQuery(variantId, periods, alpha, beta, seasonLength, includeSeasonality),
                cancellationToken)
            .ConfigureAwait(false);
    }

    [HttpPost("purchase-plan")]
    [ProducesResponseType(typeof(PurchasePlanDto), StatusCodes.Status200OK)]
    public async Task<PurchasePlanDto> GeneratePurchasePlan([FromBody] GeneratePurchasePlanQuery query, CancellationToken cancellationToken)
    {
        return await Sender.Send(query, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("scenario-simulations")]
    [ProducesResponseType(typeof(ScenarioSimulationDto), StatusCodes.Status200OK)]
    public async Task<ScenarioSimulationDto> SimulateScenario([FromBody] SimulateDemandScenarioQuery query, CancellationToken cancellationToken)
    {
        return await Sender.Send(query, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("logistics-dashboard")]
    [ProducesResponseType(typeof(LogisticsDashboardDto), StatusCodes.Status200OK)]
    public async Task<LogisticsDashboardDto> GetLogisticsDashboard([FromQuery] int planningWindowDays = 90, CancellationToken cancellationToken = default)
    {
        return await Sender.Send(new GetLogisticsDashboardQuery(planningWindowDays), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("optimization/recommendations")]
    [ProducesResponseType(typeof(OptimizationRecommendationDto), StatusCodes.Status200OK)]
    public async Task<OptimizationRecommendationDto> GenerateOptimizationRecommendations(
        [FromBody] GenerateOptimizationRecommendationsQuery query,
        CancellationToken cancellationToken)
    {
        return await Sender.Send(query, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("optimization/scenarios")]
    [ProducesResponseType(typeof(OptimizationScenarioComparisonDto), StatusCodes.Status200OK)]
    public async Task<OptimizationScenarioComparisonDto> CompareOptimizationScenarios(
        [FromBody] CompareOptimizationScenariosQuery query,
        CancellationToken cancellationToken)
    {
        return await Sender.Send(query, cancellationToken).ConfigureAwait(false);
    }
}
