using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class ProductAttributeValue : TenantEntity
{
    public int GroupId { get; set; }

    public ProductAttributeGroup? Group { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? HexColor { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
