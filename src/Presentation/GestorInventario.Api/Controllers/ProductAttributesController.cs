using GestorInventario.Application.ProductAttributes.Commands;
using GestorInventario.Application.ProductAttributes.Models;
using GestorInventario.Application.ProductAttributes.Queries;
using GestorInventario.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

[Route("api/product-attributes")]
public class ProductAttributesController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProductAttributeGroupDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<ProductAttributeGroupDto>> GetGroups(CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetProductAttributeGroupsQuery(), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(ProductAttributeGroupDto), StatusCodes.Status200OK)]
    public async Task<ProductAttributeGroupDto> GetGroup(int id, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetProductAttributeGroupByIdQuery(id), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(ProductAttributeGroupDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateProductAttributeGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(ProductAttributeGroupDto), StatusCodes.Status200OK)]
    public async Task<ProductAttributeGroupDto> UpdateGroup(int id, [FromBody] UpdateProductAttributeGroupCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { Id = id }, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGroup(int id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteProductAttributeGroupCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{groupId:int}/values")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(ProductAttributeValueDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateValue(int groupId, [FromBody] CreateProductAttributeValueCommand command, CancellationToken cancellationToken)
    {
        var value = await Sender.Send(command with { GroupId = groupId }, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetGroup), new { id = groupId }, value);
    }

    [HttpPut("{groupId:int}/values/{valueId:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(ProductAttributeValueDto), StatusCodes.Status200OK)]
    public async Task<ProductAttributeValueDto> UpdateValue(int groupId, int valueId, [FromBody] UpdateProductAttributeValueCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { GroupId = groupId, ValueId = valueId }, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{groupId:int}/values/{valueId:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteValue(int groupId, int valueId, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteProductAttributeValueCommand(groupId, valueId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
