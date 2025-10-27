using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class Category : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ParentId { get; set; }

    public Category? Parent { get; set; }

    public ICollection<Category> Children { get; set; } = new List<Category>();

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
