using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.PriceLists.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.PriceLists.Commands;

public record CreatePriceListCommand(
    string Name,
    string? Description,
    string Currency,
    IReadOnlyCollection<CreateProductPriceRequest> Prices) : IRequest<PriceListDto>;

public record CreateProductPriceRequest(int VariantId, decimal Price);

public class CreatePriceListCommandValidator : AbstractValidator<CreatePriceListCommand>
{
    public CreatePriceListCommandValidator()
    {
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

public class CreatePriceListCommandHandler : IRequestHandler<CreatePriceListCommand, PriceListDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreatePriceListCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<PriceListDto> Handle(CreatePriceListCommand request, CancellationToken cancellationToken)
    {
        var priceList = new PriceList
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Currency = request.Currency.Trim()
        };

        var variantIds = request.Prices.Select(price => price.VariantId).ToHashSet();

        var existingVariants = await context.ProductVariants
            .Where(variant => variantIds.Contains(variant.Id))
            .ToDictionaryAsync(variant => variant.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var price in request.Prices)
        {
            if (!existingVariants.ContainsKey(price.VariantId))
            {
                throw new NotFoundException(nameof(ProductVariant), price.VariantId);
            }

            priceList.ProductPrices.Add(new ProductPrice
            {
                VariantId = price.VariantId,
                Price = price.Price
            });
        }

        context.PriceLists.Add(priceList);
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
