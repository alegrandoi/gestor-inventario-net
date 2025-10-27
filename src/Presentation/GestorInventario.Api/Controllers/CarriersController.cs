using GestorInventario.Application.Carriers.Commands;
using GestorInventario.Application.Carriers.Models;
using GestorInventario.Application.Carriers.Queries;
using GestorInventario.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

public class CarriersController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<CarrierDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<CarrierDto>> GetCarriers(CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetCarriersQuery(), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(CarrierDto), StatusCodes.Status200OK)]
    public async Task<CarrierDto> GetCarrier(int id, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetCarrierByIdQuery(id), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(CarrierDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCarrier([FromBody] CreateCarrierCommand command, CancellationToken cancellationToken)
    {
        var carrier = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetCarrier), new { id = carrier.Id }, carrier);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(CarrierDto), StatusCodes.Status200OK)]
    public async Task<CarrierDto> UpdateCarrier(int id, [FromBody] UpdateCarrierCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { Id = id }, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCarrier(int id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteCarrierCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
