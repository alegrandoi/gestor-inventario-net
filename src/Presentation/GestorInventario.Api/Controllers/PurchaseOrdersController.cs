using GestorInventario.Application.PurchaseOrders.Commands;
using GestorInventario.Application.PurchaseOrders.Models;
using GestorInventario.Application.PurchaseOrders.Queries;
using GestorInventario.Domain.Constants;
using GestorInventario.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

public class PurchaseOrdersController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<PurchaseOrderDto>> GetPurchaseOrders(
        [FromQuery] int? supplierId,
        [FromQuery] PurchaseOrderStatus? status,
        CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetPurchaseOrdersQuery(supplierId, status), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status200OK)]
    public async Task<PurchaseOrderDto> GetPurchaseOrder(int id, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetPurchaseOrderByIdQuery(id), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetPurchaseOrder), new { id = order.Id }, order);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status200OK)]
    public async Task<PurchaseOrderDto> UpdateStatus(int id, [FromBody] UpdatePurchaseOrderStatusCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { OrderId = id }, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePurchaseOrder(int id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeletePurchaseOrderCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
