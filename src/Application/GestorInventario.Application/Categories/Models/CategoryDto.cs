using System.Collections.Generic;

namespace GestorInventario.Application.Categories.Models;

public record CategoryDto(
    int Id,
    string Name,
    string? Description,
    int? ParentId,
    IReadOnlyCollection<CategoryDto> Children);
