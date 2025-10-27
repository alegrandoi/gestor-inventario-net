using GestorInventario.Application.ShippingRates.Commands;
using GestorInventario.Application.ShippingRates.Models;
using GestorInventario.Application.ShippingRates.Queries;
using GestorInventario.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

public class ShippingRatesController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ShippingRateDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ShippingRateDto>> GetShippingRates(CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetShippingRatesQuery(), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(ShippingRateDto), StatusCodes.Status200OK)]
    public async Task<ShippingRateDto> GetShippingRate(int id, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetShippingRateByIdQuery(id), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator}")]
    [ProducesResponseType(typeof(ShippingRateDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateShippingRate([FromBody] CreateShippingRateCommand command, CancellationToken cancellationToken)
    {
        var rate = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetShippingRate), new { id = rate.Id }, rate);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator}")]
    [ProducesResponseType(typeof(ShippingRateDto), StatusCodes.Status200OK)]
    public async Task<ShippingRateDto> UpdateShippingRate(int id, [FromBody] UpdateShippingRateCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { Id = id }, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteShippingRate(int id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteShippingRateCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
