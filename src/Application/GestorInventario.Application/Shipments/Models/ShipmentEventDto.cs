namespace GestorInventario.Application.Shipments.Models;

public record ShipmentEventDto(
    int Id,
    string Status,
    string? Location,
    string? Description,
    DateTime EventDate,
    DateTime CreatedAt);
