using GestorInventario.Application.Tenants.Commands;
using GestorInventario.Application.Tenants.Models;
using GestorInventario.Application.Tenants.Queries;
using GestorInventario.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Administrator)]
public class TenantsController : ControllerBase
{
    private readonly IMediator mediator;

    public TenantsController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TenantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TenantDto>>> GetTenants([FromQuery] bool includeInactive = false)
    {
        var tenants = await mediator.Send(new GetTenantsQuery(includeInactive)).ConfigureAwait(false);
        return Ok(tenants);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantDto>> GetTenant(int id)
    {
        var tenant = await mediator.Send(new GetTenantByIdQuery(id)).ConfigureAwait(false);
        return Ok(tenant);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantCommand command)
    {
        var tenant = await mediator.Send(command).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantDto>> UpdateTenant(int id, [FromBody] UpdateTenantCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("El identificador del inquilino no coincide con el cuerpo de la solicitud.");
        }

        var tenant = await mediator.Send(command).ConfigureAwait(false);
        return Ok(tenant);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTenant(int id)
    {
        await mediator.Send(new DeleteTenantCommand(id)).ConfigureAwait(false);
        return NoContent();
    }
}
