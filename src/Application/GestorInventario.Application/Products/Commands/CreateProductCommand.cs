using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Products.Events;
using GestorInventario.Application.Products.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;

namespace GestorInventario.Application.Products.Commands;

public record CreateProductCommand(
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
    IReadOnlyCollection<CreateProductVariantRequest> Variants,
    IReadOnlyCollection<CreateProductImageRequest> Images) : IRequest<ProductDto>;

public record CreateProductVariantRequest(string Sku, string Attributes, decimal? Price, string? Barcode);

public record CreateProductImageRequest(string ImageUrl, string? AltText);

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
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
            .NotEmpty().WithMessage("El producto debe incluir al menos una variante.");

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

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IPublisher publisher;

    public CreateProductCommandHandler(IGestorInventarioDbContext context, IPublisher publisher)
    {
        this.context = context;
        this.publisher = publisher;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
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
            throw new ApplicationValidationException("El producto debe incluir al menos una variante activa.");
        }

        var duplicatedSku = sanitizedVariants
            .GroupBy(variant => variant.Sku, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicatedSku is not null)
        {
            throw new ApplicationValidationException($"El SKU '{duplicatedSku.Key}' se repite en la solicitud.");
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

        var exists = await context.Products
            .AnyAsync(product => product.Code == normalizedCode, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new ApplicationValidationException($"The product code '{normalizedCode}' is already in use.");
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

        var requestedSkus = sanitizedVariants
            .Select(variant => variant.Sku)
            .ToList();

        if (requestedSkus.Count > 0)
        {
            var conflictingSkus = await context.ProductVariants
                .AsNoTracking()
                .Where(variant => requestedSkus.Contains(variant.Sku))
                .Select(variant => variant.Sku)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (conflictingSkus.Count > 0)
            {
                var formatted = string.Join(", ", conflictingSkus.Distinct(StringComparer.OrdinalIgnoreCase));
                throw new ApplicationValidationException($"Los SKU {formatted} ya están en uso en el catálogo.");
            }
        }

        var product = new Product
        {
            Code = normalizedCode,
            Name = normalizedName,
            Description = request.Description?.Trim(),
            CategoryId = request.CategoryId,
            DefaultPrice = request.DefaultPrice,
            Currency = normalizedCurrency,
            TaxRateId = request.TaxRateId,
            IsActive = request.IsActive,
            WeightKg = request.WeightKg,
            HeightCm = request.HeightCm,
            WidthCm = request.WidthCm,
            LengthCm = request.LengthCm,
            LeadTimeDays = request.LeadTimeDays,
            SafetyStock = request.SafetyStock,
            ReorderPoint = request.ReorderPoint,
            ReorderQuantity = request.ReorderQuantity,
            RequiresSerialTracking = request.RequiresSerialTracking
        };

        foreach (var variant in sanitizedVariants)
        {
            product.Variants.Add(new ProductVariant
            {
                Sku = variant.Sku,
                Attributes = variant.Attributes,
                Price = variant.Price,
                Barcode = variant.Barcode
            });
        }

        foreach (var image in sanitizedImages)
        {
            product.Images.Add(new ProductImage
            {
                ImageUrl = image.ImageUrl,
                AltText = image.AltText
            });
        }

        var warehouseIds = await context.Warehouses
            .AsNoTracking()
            .Select(warehouse => warehouse.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        context.Products.Add(product);

        if (warehouseIds.Count > 0)
        {
            var minStockLevel = request.ReorderPoint ?? request.SafetyStock ?? 0m;

            foreach (var variant in product.Variants)
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

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await publisher.Publish(
            new ProductCreatedDomainEvent(
                product.Id,
                product.Code,
                product.Name,
                product.DefaultPrice,
                product.Currency,
                product.IsActive),
            cancellationToken).ConfigureAwait(false);

        await publisher.Publish(
            new ProductCatalogChangedDomainEvent(
                product.Id,
                product.Code,
                product.Name,
                product.DefaultPrice,
                product.Currency,
                product.IsActive,
                ProductCatalogChangeType.Created),
            cancellationToken).ConfigureAwait(false);

        return product.ToDto();
    }
}
