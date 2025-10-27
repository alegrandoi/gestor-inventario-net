using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class Branch : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? TimeZone { get; set; }

    public string? Locale { get; set; }

    public string? Currency { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;
}
