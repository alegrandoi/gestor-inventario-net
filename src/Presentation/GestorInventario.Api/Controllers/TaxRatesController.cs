using GestorInventario.Application.TaxRates.Commands;
using GestorInventario.Application.TaxRates.Models;
using GestorInventario.Application.TaxRates.Queries;
using GestorInventario.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

public class TaxRatesController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TaxRateDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<TaxRateDto>> GetTaxRates(CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetTaxRatesQuery(), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(TaxRateDto), StatusCodes.Status200OK)]
    public async Task<TaxRateDto> GetTaxRate(int id, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetTaxRateByIdQuery(id), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator}")]
    [ProducesResponseType(typeof(TaxRateDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTaxRate([FromBody] CreateTaxRateCommand command, CancellationToken cancellationToken)
    {
        var taxRate = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetTaxRate), new { id = taxRate.Id }, taxRate);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator}")]
    [ProducesResponseType(typeof(TaxRateDto), StatusCodes.Status200OK)]
    public async Task<TaxRateDto> UpdateTaxRate(int id, [FromBody] UpdateTaxRateCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { Id = id }, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTaxRate(int id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteTaxRateCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
