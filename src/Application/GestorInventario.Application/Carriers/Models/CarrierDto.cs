namespace GestorInventario.Application.Carriers.Models;

public record CarrierDto(
    int Id,
    string Name,
    string? ContactName,
    string? Email,
    string? Phone,
    string? TrackingUrl,
    string? Notes);
