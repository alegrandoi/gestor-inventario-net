using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class Role : Entity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
