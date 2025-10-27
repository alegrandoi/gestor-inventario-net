using FluentValidation;
using GestorInventario.Application.Carriers.Models;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Carriers.Commands;

public record CreateCarrierCommand(
    string Name,
    string? ContactName,
    string? Email,
    string? Phone,
    string? TrackingUrl,
    string? Notes) : IRequest<CarrierDto>;

public class CreateCarrierCommandValidator : AbstractValidator<CreateCarrierCommand>
{
    public CreateCarrierCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.ContactName)
            .MaximumLength(150);

        RuleFor(command => command.Email)
            .EmailAddress()
            .When(command => !string.IsNullOrWhiteSpace(command.Email));

        RuleFor(command => command.Phone)
            .MaximumLength(50);

        RuleFor(command => command.TrackingUrl)
            .MaximumLength(300);

        RuleFor(command => command.Notes)
            .MaximumLength(500);
    }
}

public class CreateCarrierCommandHandler : IRequestHandler<CreateCarrierCommand, CarrierDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreateCarrierCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<CarrierDto> Handle(CreateCarrierCommand request, CancellationToken cancellationToken)
    {
        var carrier = new Carrier
        {
            Name = request.Name.Trim(),
            ContactName = request.ContactName?.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            TrackingUrl = request.TrackingUrl?.Trim(),
            Notes = request.Notes?.Trim()
        };

        context.Carriers.Add(carrier);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return carrier.ToDto();
    }
}
