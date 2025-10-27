namespace GestorInventario.Application.Suppliers.Models;

public record SupplierDto(
    int Id,
    string Name,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes);
