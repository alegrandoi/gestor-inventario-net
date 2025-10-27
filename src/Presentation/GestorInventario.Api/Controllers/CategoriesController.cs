using GestorInventario.Application.Categories.Commands;
using GestorInventario.Application.Categories.Models;
using GestorInventario.Application.Categories.Queries;
using GestorInventario.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

public class CategoriesController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<CategoryDto>> GetCategories(CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetCategoriesQuery(), cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager},{RoleNames.Planner}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    public async Task<CategoryDto> GetCategory(int id, CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetCategoryByIdQuery(id), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    public async Task<CategoryDto> UpdateCategory(int id, [FromBody] UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { Id = id }, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.InventoryManager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteCategoryCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
