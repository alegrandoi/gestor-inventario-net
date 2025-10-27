using GestorInventario.Application.Customers.Commands;
using GestorInventario.Application.Customers.Models;
using GestorInventario.Application.Customers.Queries;
using GestorInventario.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

public class CustomersController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<CustomerDto>> GetCustomers([FromQuery] string? searchTerm, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetCustomersQuery(searchTerm), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    public async Task<CustomerDto> GetCustomer(int id, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetCustomerByIdQuery(id), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    public async Task<CustomerDto> UpdateCustomer(int id, [FromBody] UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { Id = id }, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCustomer(int id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteCustomerCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
