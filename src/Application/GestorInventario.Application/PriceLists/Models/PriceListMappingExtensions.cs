using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.PriceLists.Models;

public static class PriceListMappingExtensions
{
    public static PriceListDto ToDto(this PriceList priceList)
    {
        var prices = priceList.ProductPrices
            .Select(price => new ProductPriceDto(
                price.Id,
                price.VariantId,
                price.Price,
                price.Variant?.Sku ?? string.Empty,
                price.Variant?.Product?.Name ?? string.Empty))
            .ToList();

        return new PriceListDto(
            priceList.Id,
            priceList.Name,
            priceList.Description,
            priceList.Currency,
            prices);
    }
}
