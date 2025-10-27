namespace GestorInventario.Application.Analytics.Models;

public record ShipmentTrendPointDto(
    string Date,
    int Total,
    int Delivered,
    int InTransit);
