using FluentValidation;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.TaxRates.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;

namespace GestorInventario.Application.TaxRates.Commands;

public record CreateTaxRateCommand(string Name, decimal Rate, string? Region, string? Description) : IRequest<TaxRateDto>;

public class CreateTaxRateCommandValidator : AbstractValidator<CreateTaxRateCommand>
{
    public CreateTaxRateCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Rate)
            .InclusiveBetween(0, 100);

        RuleFor(command => command.Region)
            .MaximumLength(50);

        RuleFor(command => command.Description)
            .MaximumLength(200);
    }
}

public class CreateTaxRateCommandHandler : IRequestHandler<CreateTaxRateCommand, TaxRateDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreateTaxRateCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<TaxRateDto> Handle(CreateTaxRateCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        var normalizedRegion = request.Region?.Trim();

        var duplicateExists = await context.TaxRates
            .AnyAsync(
                rate => rate.Name == normalizedName && rate.Region == normalizedRegion,
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateExists)
        {
            throw new ApplicationValidationException("Ya existe una tarifa con el mismo nombre y región.");
        }

        var taxRate = new TaxRate
        {
            Name = normalizedName,
            Rate = request.Rate,
            Region = normalizedRegion,
            Description = request.Description?.Trim()
        };

        context.TaxRates.Add(taxRate);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return taxRate.ToDto();
    }
}
