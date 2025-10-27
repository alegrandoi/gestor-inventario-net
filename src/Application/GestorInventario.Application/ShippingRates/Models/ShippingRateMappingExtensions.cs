using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.ShippingRates.Models;

public static class ShippingRateMappingExtensions
{
    public static ShippingRateDto ToDto(this ShippingRate shippingRate) =>
        new(
            shippingRate.Id,
            shippingRate.Name,
            shippingRate.BaseCost,
            shippingRate.CostPerWeight,
            shippingRate.CostPerDistance,
            shippingRate.Currency,
            shippingRate.Description);
}
