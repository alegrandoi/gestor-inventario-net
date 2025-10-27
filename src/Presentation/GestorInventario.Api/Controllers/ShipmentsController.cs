using GestorInventario.Application.Shipments.Commands;
using GestorInventario.Application.Shipments.Models;
using GestorInventario.Application.Shipments.Queries;
using GestorInventario.Domain.Constants;
using GestorInventario.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

public class ShipmentsController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ShipmentDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ShipmentDto>> GetShipments(
        [FromQuery] int? salesOrderId,
        [FromQuery] ShipmentStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? warehouseId,
        CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetShipmentsQuery(salesOrderId, status, from, to, warehouseId), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    public async Task<ShipmentDto> GetShipment(int id, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetShipmentByIdQuery(id), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentCommand command, CancellationToken cancellationToken)
    {
        var shipment = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetShipment), new { id = shipment.Id }, shipment);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    public async Task<ShipmentDto> UpdateShipmentStatus(int id, [FromBody] UpdateShipmentStatusRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateShipmentStatusCommand(id, request.Status, request.DeliveredAt, request.EstimatedDeliveryDate, request.Notes);
        return await Sender.Send(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("{id:int}/events")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(ShipmentDto), StatusCodes.Status200OK)]
    public async Task<ShipmentDto> RecordEvent(int id, [FromBody] RecordShipmentEventRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordShipmentEventCommand(id, request.Status, request.EventDate, request.Location, request.Description);
        return await Sender.Send(command, cancellationToken).ConfigureAwait(false);
    }
}

public record UpdateShipmentStatusRequest(
    ShipmentStatus Status,
    DateTime? DeliveredAt,
    DateTime? EstimatedDeliveryDate,
    string? Notes);

public record RecordShipmentEventRequest(
    string Status,
    DateTime EventDate,
    string? Location,
    string? Description);
