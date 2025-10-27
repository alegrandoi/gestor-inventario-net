using System.Linq;
using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.ProductAttributes.Models;

public static class ProductAttributeMappingExtensions
{
    public static ProductAttributeGroupDto ToDto(this ProductAttributeGroup group)
    {
        var values = group.Values
            .OrderBy(value => value.DisplayOrder)
            .ThenBy(value => value.Name)
            .Select(value => value.ToDto())
            .ToArray();

        return new ProductAttributeGroupDto(
            group.Id,
            group.Name,
            group.Slug,
            group.Description,
            group.AllowCustomValues,
            values);
    }

    public static ProductAttributeValueDto ToDto(this ProductAttributeValue value) =>
        new(
            value.Id,
            value.Name,
            value.Description,
            value.HexColor,
            value.DisplayOrder,
            value.IsActive);
}
