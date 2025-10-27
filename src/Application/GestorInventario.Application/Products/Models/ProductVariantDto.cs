namespace GestorInventario.Application.Products.Models;

public record ProductVariantDto(
    int Id,
    string Sku,
    string Attributes,
    decimal? Price,
    string? Barcode);
