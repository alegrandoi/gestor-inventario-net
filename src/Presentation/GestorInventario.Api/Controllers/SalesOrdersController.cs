using GestorInventario.Application.SalesOrders.Commands;
using GestorInventario.Application.SalesOrders.Models;
using GestorInventario.Application.SalesOrders.Queries;
using GestorInventario.Domain.Constants;
using GestorInventario.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

public class SalesOrdersController : ApiControllerBase
{
    [HttpGet]
    [HttpGet("~/api/orders")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SalesOrderDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<SalesOrderDto>> GetSalesOrders(
        [FromQuery] int? customerId,
        [FromQuery] SalesOrderStatus? status,
        CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetSalesOrdersQuery(customerId, status), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}")]
    [HttpGet("~/api/orders/{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status200OK)]
    public async Task<SalesOrderDto> GetSalesOrder(int id, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetSalesOrderByIdQuery(id), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [HttpPost("~/api/orders")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSalesOrder([FromBody] CreateSalesOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetSalesOrder), new { id = order.Id }, order);
    }

    [HttpPut("{id:int}/status")]
    [HttpPut("~/api/orders/{id:int}/status")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status200OK)]
    public async Task<SalesOrderDto> UpdateStatus(int id, [FromBody] UpdateSalesOrderStatusCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { OrderId = id }, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    [HttpDelete("~/api/orders/{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSalesOrder(int id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteSalesOrderCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
