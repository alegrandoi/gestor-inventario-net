using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class Tenant : AuditableEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? DefaultCulture { get; set; }

    public string? DefaultCurrency { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
