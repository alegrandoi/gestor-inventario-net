using GestorInventario.Application.Inventory.Commands;
using GestorInventario.Application.Inventory.Models;
using GestorInventario.Application.Inventory.Queries;
using GestorInventario.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

public class InventoryController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<InventoryStockDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<InventoryStockDto>> GetInventory(
        [FromQuery] int? warehouseId,
        [FromQuery] int? variantId,
        [FromQuery] bool includeBelowMinimum,
        CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetInventoryOverviewQuery(warehouseId, variantId, includeBelowMinimum), cancellationToken)
            .ConfigureAwait(false);
    }

    [HttpPost("adjust")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<InventoryStockDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<InventoryStockDto>> AdjustInventory(
        [FromBody] AdjustInventoryCommand command,
        CancellationToken cancellationToken)
    {
        return await Sender.Send(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("replenishment-plan")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(ReplenishmentPlanDto), StatusCodes.Status200OK)]
    public async Task<ReplenishmentPlanDto> GetReplenishmentPlan(
        [FromQuery] DateTime? fromDate,
        [FromQuery] int planningWindowDays,
        CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetReplenishmentPlanQuery(fromDate, planningWindowDays), cancellationToken).ConfigureAwait(false);
    }
}
