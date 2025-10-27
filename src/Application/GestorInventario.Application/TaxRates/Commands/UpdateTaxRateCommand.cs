using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.TaxRates.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;

namespace GestorInventario.Application.TaxRates.Commands;

public record UpdateTaxRateCommand(int Id, string Name, decimal Rate, string? Region, string? Description) : IRequest<TaxRateDto>;

public class UpdateTaxRateCommandValidator : AbstractValidator<UpdateTaxRateCommand>
{
    public UpdateTaxRateCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);

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

public class UpdateTaxRateCommandHandler : IRequestHandler<UpdateTaxRateCommand, TaxRateDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdateTaxRateCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<TaxRateDto> Handle(UpdateTaxRateCommand request, CancellationToken cancellationToken)
    {
        var taxRate = await context.TaxRates.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (taxRate is null)
        {
            throw new NotFoundException(nameof(TaxRate), request.Id);
        }

        var normalizedName = request.Name.Trim();
        var normalizedRegion = request.Region?.Trim();
        var normalizedDescription = request.Description?.Trim();

        var duplicateExists = await context.TaxRates
            .AnyAsync(
                rate => rate.Id != request.Id
                    && rate.Name == normalizedName
                    && rate.Region == normalizedRegion,
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateExists)
        {
            throw new ApplicationValidationException("Ya existe otra tarifa con el mismo nombre y región.");
        }

        taxRate.Name = normalizedName;
        taxRate.Rate = request.Rate;
        taxRate.Region = normalizedRegion;
        taxRate.Description = normalizedDescription;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return taxRate.ToDto();
    }
}
