using GestorInventario.Domain.Entities;

namespace GestorInventario.Domain.Abstractions;

public interface ITenantScopedEntity
{
    int TenantId { get; set; }

    Tenant? Tenant { get; set; }
}
