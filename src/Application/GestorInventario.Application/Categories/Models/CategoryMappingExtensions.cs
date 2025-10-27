using System;
using System.Collections.Generic;
using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.Categories.Models;

public static class CategoryMappingExtensions
{
    public static CategoryDto ToDto(this Category category, IReadOnlyCollection<CategoryDto>? children = null) =>
        new(category.Id, category.Name, category.Description, category.ParentId, children ?? Array.Empty<CategoryDto>());
}
