namespace GestorInventario.Application.ProductAttributes.Models;

public record ProductAttributeValueDto(
    int Id,
    string Name,
    string? Description,
    string? HexColor,
    int DisplayOrder,
    bool IsActive);

public record ProductAttributeGroupDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    bool AllowCustomValues,
    IReadOnlyCollection<ProductAttributeValueDto> Values);
