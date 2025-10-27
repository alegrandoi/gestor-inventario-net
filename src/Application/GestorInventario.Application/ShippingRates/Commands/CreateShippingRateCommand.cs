using FluentValidation;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.ShippingRates.Models;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.ShippingRates.Commands;

public record CreateShippingRateCommand(
    string Name,
    decimal BaseCost,
    decimal? CostPerWeight,
    decimal? CostPerDistance,
    string Currency,
    string? Description) : IRequest<ShippingRateDto>;

public class CreateShippingRateCommandValidator : AbstractValidator<CreateShippingRateCommand>
{
    public CreateShippingRateCommandValidator()
    {
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

public class CreateShippingRateCommandHandler : IRequestHandler<CreateShippingRateCommand, ShippingRateDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreateShippingRateCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ShippingRateDto> Handle(CreateShippingRateCommand request, CancellationToken cancellationToken)
    {
        var shippingRate = new ShippingRate
        {
            Name = request.Name.Trim(),
            BaseCost = request.BaseCost,
            CostPerWeight = request.CostPerWeight,
            CostPerDistance = request.CostPerDistance,
            Currency = request.Currency.Trim(),
            Description = request.Description?.Trim()
        };

        context.ShippingRates.Add(shippingRate);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return shippingRate.ToDto();
    }
}
