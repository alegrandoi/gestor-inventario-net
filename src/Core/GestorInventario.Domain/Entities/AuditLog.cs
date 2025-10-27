using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class AuditLog : TenantEntity
{
    public string EntityName { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? Changes { get; set; }

    public int? UserId { get; set; }

    public User? User { get; set; }
}
