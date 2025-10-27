namespace GestorInventario.Application.ShippingRates.Models;

public record ShippingRateDto(
    int Id,
    string Name,
    decimal BaseCost,
    decimal? CostPerWeight,
    decimal? CostPerDistance,
    string Currency,
    string? Description);
