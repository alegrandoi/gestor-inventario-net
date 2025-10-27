using GestorInventario.Application.Inventory.Commands.WarehouseProductVariants;
using GestorInventario.Application.Inventory.Models;
using GestorInventario.Application.Inventory.Queries;
using GestorInventario.Application.Warehouses.Commands;
using GestorInventario.Application.Warehouses.Models;
using GestorInventario.Application.Warehouses.Queries;
using GestorInventario.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

public class WarehousesController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<WarehouseDto>> GetWarehouses(CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetWarehousesQuery(), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{warehouseId:int}/product-variants")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<WarehouseProductVariantAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<WarehouseProductVariantAssignmentDto>> GetWarehouseProductVariants(
        int warehouseId,
        CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetWarehouseProductVariantsQuery(warehouseId), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    public async Task<WarehouseDto> GetWarehouse(int id, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetWarehouseByIdQuery(id), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("product-variants/by-variant/{variantId:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<WarehouseProductVariantAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<WarehouseProductVariantAssignmentDto>> GetVariantAssignments(
        int variantId,
        CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetVariantWarehouseAssignmentsQuery(variantId), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseCommand command, CancellationToken cancellationToken)
    {
        var warehouse = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, warehouse);
    }

    [HttpPost("{warehouseId:int}/product-variants")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(WarehouseProductVariantAssignmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WarehouseProductVariantAssignmentDto>> AssignProductVariant(
        int warehouseId,
        [FromBody] CreateWarehouseProductVariantAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        var assignment = await Sender.Send(command with { WarehouseId = warehouseId }, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetWarehouseProductVariants), new { warehouseId }, assignment);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    public async Task<WarehouseDto> UpdateWarehouse(int id, [FromBody] UpdateWarehouseCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { Id = id }, cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("{warehouseId:int}/product-variants/{assignmentId:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(WarehouseProductVariantAssignmentDto), StatusCodes.Status200OK)]
    public async Task<WarehouseProductVariantAssignmentDto> UpdateWarehouseProductVariant(
        int warehouseId,
        int assignmentId,
        [FromBody] UpdateWarehouseProductVariantAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { Id = assignmentId, WarehouseId = warehouseId }, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteWarehouse(int id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteWarehouseCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{warehouseId:int}/product-variants/{assignmentId:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteWarehouseProductVariant(int warehouseId, int assignmentId, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteWarehouseProductVariantAssignmentCommand(assignmentId, warehouseId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
