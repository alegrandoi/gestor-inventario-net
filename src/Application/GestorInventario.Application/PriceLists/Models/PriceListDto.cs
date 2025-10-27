using GestorInventario.Application.PriceLists.Models;

namespace GestorInventario.Application.PriceLists.Models;

public record PriceListDto(
    int Id,
    string Name,
    string? Description,
    string Currency,
    IReadOnlyCollection<ProductPriceDto> Prices);
