using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.PriceLists.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.PriceLists.Commands;

public record UpdatePriceListCommand(
    int Id,
    string Name,
    string? Description,
    string Currency,
    IReadOnlyCollection<UpdateProductPriceRequest> Prices) : IRequest<PriceListDto>;

public record UpdateProductPriceRequest(int? Id, int VariantId, decimal Price);

public class UpdatePriceListCommandValidator : AbstractValidator<UpdatePriceListCommand>
{
    public UpdatePriceListCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Currency)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(command => command.Description)
            .MaximumLength(200);

        RuleForEach(command => command.Prices)
            .ChildRules(price =>
            {
                price.RuleFor(p => p.VariantId)
                    .GreaterThan(0);

                price.RuleFor(p => p.Price)
                    .GreaterThanOrEqualTo(0);
            });
    }
}

public class UpdatePriceListCommandHandler : IRequestHandler<UpdatePriceListCommand, PriceListDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdatePriceListCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<PriceListDto> Handle(UpdatePriceListCommand request, CancellationToken cancellationToken)
    {
        var priceList = await context.PriceLists
            .Include(list => list.ProductPrices)
            .FirstOrDefaultAsync(list => list.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (priceList is null)
        {
            throw new NotFoundException(nameof(PriceList), request.Id);
        }

        priceList.Name = request.Name.Trim();
        priceList.Description = request.Description?.Trim();
        priceList.Currency = request.Currency.Trim();

        var variantIds = request.Prices.Select(price => price.VariantId).ToHashSet();
        var existingVariants = await context.ProductVariants
            .Where(variant => variantIds.Contains(variant.Id))
            .ToDictionaryAsync(variant => variant.Id, cancellationToken)
            .ConfigureAwait(false);

        var pricesToRemove = priceList.ProductPrices
            .Where(existing => request.Prices.All(update => update.Id != existing.Id))
            .ToList();

        foreach (var price in pricesToRemove)
        {
            priceList.ProductPrices.Remove(price);
        }

        foreach (var priceRequest in request.Prices)
        {
            if (!existingVariants.ContainsKey(priceRequest.VariantId))
            {
                throw new NotFoundException(nameof(ProductVariant), priceRequest.VariantId);
            }

            if (priceRequest.Id.HasValue)
            {
                var existing = priceList.ProductPrices.FirstOrDefault(price => price.Id == priceRequest.Id.Value);
                if (existing is null)
                {
                    continue;
                }

                existing.VariantId = priceRequest.VariantId;
                existing.Price = priceRequest.Price;
            }
            else
            {
                priceList.ProductPrices.Add(new ProductPrice
                {
                    VariantId = priceRequest.VariantId,
                    Price = priceRequest.Price
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        priceList = await context.PriceLists
            .Include(list => list.ProductPrices)
                .ThenInclude(price => price.Variant)
                    .ThenInclude(variant => variant!.Product)
            .FirstAsync(list => list.Id == priceList.Id, cancellationToken)
            .ConfigureAwait(false);

        return priceList.ToDto();
    }
}
