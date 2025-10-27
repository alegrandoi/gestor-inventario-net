using GestorInventario.Application.Suppliers.Commands;
using GestorInventario.Application.Suppliers.Models;
using GestorInventario.Application.Suppliers.Queries;
using GestorInventario.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

public class SuppliersController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SupplierDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<SupplierDto>> GetSuppliers([FromQuery] string? searchTerm, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetSuppliersQuery(searchTerm), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    public async Task<SupplierDto> GetSupplier(int id, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetSupplierByIdQuery(id), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierCommand command, CancellationToken cancellationToken)
    {
        var supplier = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    public async Task<SupplierDto> UpdateSupplier(int id, [FromBody] UpdateSupplierCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { Id = id }, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Planner}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSupplier(int id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteSupplierCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
