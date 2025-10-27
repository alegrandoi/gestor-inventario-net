using GestorInventario.Application.PriceLists.Commands;
using GestorInventario.Application.PriceLists.Models;
using GestorInventario.Application.PriceLists.Queries;
using GestorInventario.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

public class PriceListsController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PriceListDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<PriceListDto>> GetPriceLists(CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetPriceListsQuery(), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status200OK)]
    public async Task<PriceListDto> GetPriceList(int id, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetPriceListByIdQuery(id), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePriceList([FromBody] CreatePriceListCommand command, CancellationToken cancellationToken)
    {
        var priceList = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetPriceList), new { id = priceList.Id }, priceList);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status200OK)]
    public async Task<PriceListDto> UpdatePriceList(int id, [FromBody] UpdatePriceListCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { Id = id }, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePriceList(int id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeletePriceListCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
