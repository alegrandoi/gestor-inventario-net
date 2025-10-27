namespace GestorInventario.Application.PriceLists.Models;

public record ProductPriceDto(int Id, int VariantId, decimal Price, string VariantSku, string ProductName);
