namespace GestorInventario.Application.Common.Interfaces;

public interface ICurrentTenantService
{
    int? TenantId { get; }

    string? TenantCode { get; }

    bool HasTenant => TenantId.HasValue;
}
