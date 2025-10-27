using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Products.Events;
using GestorInventario.Application.Products.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;

namespace GestorInventario.Application.Products.Commands;

public record UpdateProductCommand(
    int Id,
    string Code,
    string Name,
    string? Description,
    int? CategoryId,
    decimal DefaultPrice,
    string Currency,
    int? TaxRateId,
    bool IsActive,
    decimal WeightKg,
    decimal? HeightCm,
    decimal? WidthCm,
    decimal? LengthCm,
    int? LeadTimeDays,
    decimal? SafetyStock,
    decimal? ReorderPoint,
    decimal? ReorderQuantity,
    bool RequiresSerialTracking,
    IReadOnlyCollection<UpdateProductVariantRequest> Variants,
    IReadOnlyCollection<UpdateProductImageRequest> Images) : IRequest<ProductDto>;

public record UpdateProductVariantRequest(int Id, string Sku, string Attributes, decimal? Price, string? Barcode);

public record UpdateProductImageRequest(int Id, string ImageUrl, string? AltText);

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);

        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.Currency)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(command => command.DefaultPrice)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.WeightKg)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.HeightCm)
            .GreaterThanOrEqualTo(0)
            .When(command => command.HeightCm.HasValue);

        RuleFor(command => command.WidthCm)
            .GreaterThanOrEqualTo(0)
            .When(command => command.WidthCm.HasValue);

        RuleFor(command => command.LengthCm)
            .GreaterThanOrEqualTo(0)
            .When(command => command.LengthCm.HasValue);

        RuleFor(command => command.LeadTimeDays)
            .GreaterThanOrEqualTo(0)
            .When(command => command.LeadTimeDays.HasValue);

        RuleFor(command => command.SafetyStock)
            .GreaterThanOrEqualTo(0)
            .When(command => command.SafetyStock.HasValue);

        RuleFor(command => command.ReorderPoint)
            .GreaterThanOrEqualTo(0)
            .When(command => command.ReorderPoint.HasValue);

        RuleFor(command => command.ReorderQuantity)
            .GreaterThanOrEqualTo(0)
            .When(command => command.ReorderQuantity.HasValue);

        RuleFor(command => command.Variants)
            .NotEmpty().WithMessage("El producto debe mantener al menos una variante.");

        RuleForEach(command => command.Variants)
            .ChildRules(variant =>
            {
                variant.RuleFor(v => v.Sku)
                    .NotEmpty()
                    .MaximumLength(50);

                variant.RuleFor(v => v.Attributes)
                    .NotEmpty()
                    .MaximumLength(200);

                variant.RuleFor(v => v.Barcode)
                    .MaximumLength(50);

                variant.RuleFor(v => v.Price)
                    .GreaterThanOrEqualTo(0)
                    .When(v => v.Price.HasValue);
            });

        RuleForEach(command => command.Images)
            .ChildRules(image =>
            {
                image.RuleFor(i => i.ImageUrl)
                    .NotEmpty()
                    .MaximumLength(300);

                image.RuleFor(i => i.AltText)
                    .MaximumLength(200);
            });
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IPublisher publisher;

    public UpdateProductCommandHandler(IGestorInventarioDbContext context, IPublisher publisher)
    {
        this.context = context;
        this.publisher = publisher;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.Id);
        }

        var normalizedCode = request.Code.Trim();
        var normalizedName = request.Name.Trim();
        var normalizedCurrency = request.Currency.Trim().ToUpperInvariant();
        var sanitizedVariants = request.Variants
            .Select(variant => variant with
            {
                Sku = variant.Sku.Trim(),
                Attributes = variant.Attributes.Trim(),
                Barcode = string.IsNullOrWhiteSpace(variant.Barcode) ? null : variant.Barcode.Trim()
            })
            .ToList();

        if (sanitizedVariants.Count == 0)
        {
            throw new ApplicationValidationException("El producto debe mantener al menos una variante activa.");
        }

        var duplicatedSku = sanitizedVariants
            .GroupBy(variant => variant.Sku, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicatedSku is not null)
        {
            throw new ApplicationValidationException($"La variante con SKU '{duplicatedSku.Key}' está duplicada.");
        }

        var missingVariantIds = sanitizedVariants
            .Where(variant => variant.Id > 0 && product.Variants.All(existing => existing.Id != variant.Id))
            .Select(variant => variant.Id)
            .ToList();

        if (missingVariantIds.Count > 0)
        {
            throw new ApplicationValidationException("Se intentó modificar variantes que no pertenecen al producto actual.");
        }

        var sanitizedImages = request.Images
            .Select(image => image with
            {
                ImageUrl = image.ImageUrl.Trim(),
                AltText = string.IsNullOrWhiteSpace(image.AltText) ? null : image.AltText.Trim()
            })
            .ToList();

        var duplicatedImage = sanitizedImages
            .GroupBy(image => image.ImageUrl, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicatedImage is not null)
        {
            throw new ApplicationValidationException($"La imagen '{duplicatedImage.Key}' está duplicada.");
        }

        var missingImageIds = sanitizedImages
            .Where(image => image.Id > 0 && product.Images.All(existing => existing.Id != image.Id))
            .Select(image => image.Id)
            .ToList();

        if (missingImageIds.Count > 0)
        {
            throw new ApplicationValidationException("Se intentó modificar imágenes que no pertenecen al producto actual.");
        }

        var codeChanged = !string.Equals(product.Code, normalizedCode, StringComparison.OrdinalIgnoreCase);

        if (codeChanged)
        {
            var exists = await context.Products
                .AnyAsync(p => p.Code == normalizedCode && p.Id != request.Id, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                throw new ApplicationValidationException($"The product code '{normalizedCode}' is already in use.");
            }

            product.Code = normalizedCode;
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await context.Categories
                .AnyAsync(category => category.Id == request.CategoryId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!categoryExists)
            {
                throw new NotFoundException(nameof(Category), request.CategoryId.Value);
            }
        }

        if (request.TaxRateId.HasValue)
        {
            var taxRateExists = await context.TaxRates
                .AnyAsync(rate => rate.Id == request.TaxRateId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!taxRateExists)
            {
                throw new NotFoundException(nameof(TaxRate), request.TaxRateId.Value);
            }
        }

        product.Name = normalizedName;
        product.Description = request.Description?.Trim();
        product.CategoryId = request.CategoryId;
        product.DefaultPrice = request.DefaultPrice;
        product.Currency = normalizedCurrency;
        product.TaxRateId = request.TaxRateId;
        product.IsActive = request.IsActive;
        product.WeightKg = request.WeightKg;
        product.HeightCm = request.HeightCm;
        product.WidthCm = request.WidthCm;
        product.LengthCm = request.LengthCm;
        product.LeadTimeDays = request.LeadTimeDays;
        product.SafetyStock = request.SafetyStock;
        product.ReorderPoint = request.ReorderPoint;
        product.ReorderQuantity = request.ReorderQuantity;
        product.RequiresSerialTracking = request.RequiresSerialTracking;

        var candidateSkus = sanitizedVariants
            .Where(variant =>
            {
                var existing = product.Variants.FirstOrDefault(current => current.Id == variant.Id);
                return existing is null || !string.Equals(existing.Sku, variant.Sku, StringComparison.OrdinalIgnoreCase);
            })
            .Select(variant => variant.Sku)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidateSkus.Count > 0)
        {
            var conflictingSkus = await context.ProductVariants
                .AsNoTracking()
                .Where(variant => candidateSkus.Contains(variant.Sku) && variant.ProductId != product.Id)
                .Select(variant => variant.Sku)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (conflictingSkus.Count > 0)
            {
                var formatted = string.Join(", ", conflictingSkus.Distinct(StringComparer.OrdinalIgnoreCase));
                throw new ApplicationValidationException($"Los SKU {formatted} ya están en uso en otros productos.");
            }
        }

        var newVariants = UpdateVariants(product, sanitizedVariants);
        UpdateImages(product, sanitizedImages);

        if (newVariants.Count > 0)
        {
            var warehouseIds = await context.Warehouses
                .AsNoTracking()
                .Select(warehouse => warehouse.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (warehouseIds.Count > 0)
            {
                var minStockLevel = product.ReorderPoint ?? product.SafetyStock ?? 0m;

                foreach (var variant in newVariants)
                {
                    foreach (var warehouseId in warehouseIds)
                    {
                        context.InventoryStocks.Add(new InventoryStock
                        {
                            Variant = variant,
                            WarehouseId = warehouseId,
                            Quantity = 0,
                            ReservedQuantity = 0,
                            MinStockLevel = minStockLevel
                        });
                    }
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var updatedMinStockLevel = product.ReorderPoint ?? product.SafetyStock ?? 0m;
        var variantIds = product.Variants.Select(variant => variant.Id).ToList();

        if (variantIds.Count > 0)
        {
            var stocksToUpdate = await context.InventoryStocks
                .Where(stock => variantIds.Contains(stock.VariantId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var stock in stocksToUpdate)
            {
                stock.MinStockLevel = updatedMinStockLevel;
            }

            if (stocksToUpdate.Count > 0)
            {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        await publisher.Publish(
            new ProductCatalogChangedDomainEvent(
                product.Id,
                product.Code,
                product.Name,
                product.DefaultPrice,
                product.Currency,
                product.IsActive,
                ProductCatalogChangeType.Updated),
            cancellationToken).ConfigureAwait(false);

        return product.ToDto();
    }

    private static IReadOnlyCollection<ProductVariant> UpdateVariants(Product product, IReadOnlyCollection<UpdateProductVariantRequest> variants)
    {
        var variantsToRemove = product.Variants
            .Where(existing => variants.All(updated => updated.Id != existing.Id))
            .ToList();

        foreach (var variant in variantsToRemove)
        {
            product.Variants.Remove(variant);
        }

        var createdVariants = new List<ProductVariant>();

        foreach (var variantRequest in variants)
        {
            if (variantRequest.Id > 0)
            {
                var existing = product.Variants.FirstOrDefault(v => v.Id == variantRequest.Id);
                if (existing is null)
                {
                    continue;
                }

                existing.Sku = variantRequest.Sku;
                existing.Attributes = variantRequest.Attributes;
                existing.Price = variantRequest.Price;
                existing.Barcode = variantRequest.Barcode;
            }
            else
            {
                var created = new ProductVariant
                {
                    Sku = variantRequest.Sku,
                    Attributes = variantRequest.Attributes,
                    Price = variantRequest.Price,
                    Barcode = variantRequest.Barcode
                };

                product.Variants.Add(created);
                createdVariants.Add(created);
            }
        }

        return createdVariants;
    }

    private static void UpdateImages(Product product, IReadOnlyCollection<UpdateProductImageRequest> images)
    {
        var imagesToRemove = product.Images
            .Where(existing => images.All(updated => updated.Id != existing.Id))
            .ToList();

        foreach (var image in imagesToRemove)
        {
            product.Images.Remove(image);
        }

        foreach (var imageRequest in images)
        {
            if (imageRequest.Id > 0)
            {
                var existing = product.Images.FirstOrDefault(image => image.Id == imageRequest.Id);
                if (existing is null)
                {
                    continue;
                }

                existing.ImageUrl = imageRequest.ImageUrl;
                existing.AltText = imageRequest.AltText;
            }
            else
            {
                product.Images.Add(new ProductImage
                {
                    ImageUrl = imageRequest.ImageUrl,
                    AltText = imageRequest.AltText
                });
            }
        }
    }
}
