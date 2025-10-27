namespace GestorInventario.Application.Tenants.Models;

public record BranchDto(int Id, string Name, string Code, string? Locale, string? TimeZone, string? Currency, bool IsDefault, bool IsActive);

public record TenantDto(
    int Id,
    string Name,
    string Code,
    string? DefaultCulture,
    string? DefaultCurrency,
    bool IsActive,
    IReadOnlyCollection<BranchDto> Branches);
