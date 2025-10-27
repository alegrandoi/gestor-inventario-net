using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.ShippingRates.Models;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.ShippingRates.Commands;

public record UpdateShippingRateCommand(
    int Id,
    string Name,
    decimal BaseCost,
    decimal? CostPerWeight,
    decimal? CostPerDistance,
    string Currency,
    string? Description) : IRequest<ShippingRateDto>;

public class UpdateShippingRateCommandValidator : AbstractValidator<UpdateShippingRateCommand>
{
    public UpdateShippingRateCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.BaseCost)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.CostPerWeight)
            .GreaterThanOrEqualTo(0)
            .When(command => command.CostPerWeight.HasValue);

        RuleFor(command => command.CostPerDistance)
            .GreaterThanOrEqualTo(0)
            .When(command => command.CostPerDistance.HasValue);

        RuleFor(command => command.Currency)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(command => command.Description)
            .MaximumLength(200);
    }
}

public class UpdateShippingRateCommandHandler : IRequestHandler<UpdateShippingRateCommand, ShippingRateDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdateShippingRateCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ShippingRateDto> Handle(UpdateShippingRateCommand request, CancellationToken cancellationToken)
    {
        var shippingRate = await context.ShippingRates.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (shippingRate is null)
        {
            throw new NotFoundException(nameof(ShippingRate), request.Id);
        }

        shippingRate.Name = request.Name.Trim();
        shippingRate.BaseCost = request.BaseCost;
        shippingRate.CostPerWeight = request.CostPerWeight;
        shippingRate.CostPerDistance = request.CostPerDistance;
        shippingRate.Currency = request.Currency.Trim();
        shippingRate.Description = request.Description?.Trim();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return shippingRate.ToDto();
    }
}
