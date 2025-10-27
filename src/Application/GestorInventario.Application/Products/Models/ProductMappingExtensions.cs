using System;
using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.Products.Models;

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product)
    {
        var appliedTaxRate = product.TaxRate?.Rate;
        var finalPrice = CalculateFinalPrice(product.DefaultPrice, appliedTaxRate);

        var variants = product.Variants
            .Select(variant => new ProductVariantDto(
                variant.Id,
                variant.Sku,
                variant.Attributes,
                variant.Price,
                variant.Barcode))
            .ToList();

        var images = product.Images
            .Select(image => new ProductImageDto(
                image.Id,
                image.ImageUrl,
                image.AltText))
            .ToList();

        return new ProductDto(
            product.Id,
            product.Code,
            product.Name,
            product.Description,
            product.CategoryId,
            product.DefaultPrice,
            product.Currency,
            product.TaxRateId,
            appliedTaxRate,
            finalPrice,
            product.IsActive,
            product.RequiresSerialTracking,
            product.WeightKg,
            product.HeightCm,
            product.WidthCm,
            product.LengthCm,
            product.LeadTimeDays,
            product.SafetyStock,
            product.ReorderPoint,
            product.ReorderQuantity,
            variants,
            images);
    }

    private static decimal CalculateFinalPrice(decimal defaultPrice, decimal? taxRate)
    {
        var normalizedRate = taxRate.HasValue ? taxRate.Value / 100m : 0m;
        var finalPrice = defaultPrice * (1 + normalizedRate);
        return decimal.Round(finalPrice, 2, MidpointRounding.AwayFromZero);
    }
}
