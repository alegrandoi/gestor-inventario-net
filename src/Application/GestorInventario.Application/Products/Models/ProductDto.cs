using GestorInventario.Application.Products.Models;

namespace GestorInventario.Application.Products.Models;

public record ProductDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    int? CategoryId,
    decimal DefaultPrice,
    string Currency,
    int? TaxRateId,
    decimal? AppliedTaxRate,
    decimal FinalPrice,
    bool IsActive,
    bool RequiresSerialTracking,
    decimal WeightKg,
    decimal? HeightCm,
    decimal? WidthCm,
    decimal? LengthCm,
    int? LeadTimeDays,
    decimal? SafetyStock,
    decimal? ReorderPoint,
    decimal? ReorderQuantity,
    IReadOnlyCollection<ProductVariantDto> Variants,
    IReadOnlyCollection<ProductImageDto> Images);
