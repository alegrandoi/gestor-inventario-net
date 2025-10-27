using FluentValidation;
using GestorInventario.Application.Carriers.Models;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Carriers.Commands;

public record UpdateCarrierCommand(
    int Id,
    string Name,
    string? ContactName,
    string? Email,
    string? Phone,
    string? TrackingUrl,
    string? Notes) : IRequest<CarrierDto>;

public class UpdateCarrierCommandValidator : AbstractValidator<UpdateCarrierCommand>
{
    public UpdateCarrierCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);

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

public class UpdateCarrierCommandHandler : IRequestHandler<UpdateCarrierCommand, CarrierDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdateCarrierCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<CarrierDto> Handle(UpdateCarrierCommand request, CancellationToken cancellationToken)
    {
        var carrier = await context.Carriers.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);
        if (carrier is null)
        {
            throw new NotFoundException(nameof(Carrier), request.Id);
        }

        carrier.Name = request.Name.Trim();
        carrier.ContactName = request.ContactName?.Trim();
        carrier.Email = request.Email?.Trim();
        carrier.Phone = request.Phone?.Trim();
        carrier.TrackingUrl = request.TrackingUrl?.Trim();
        carrier.Notes = request.Notes?.Trim();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return carrier.ToDto();
    }
}
