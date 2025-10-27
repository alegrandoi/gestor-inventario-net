using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Shipments.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Shipments.Commands;

public record RecordShipmentEventCommand(
    int ShipmentId,
    string Status,
    DateTime EventDate,
    string? Location,
    string? Description) : IRequest<ShipmentDto>;

public class RecordShipmentEventCommandValidator : AbstractValidator<RecordShipmentEventCommand>
{
    public RecordShipmentEventCommandValidator()
    {
        RuleFor(command => command.ShipmentId)
            .GreaterThan(0);

        RuleFor(command => command.Status)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Location)
            .MaximumLength(200);

        RuleFor(command => command.Description)
            .MaximumLength(500);
    }
}

public class RecordShipmentEventCommandHandler : IRequestHandler<RecordShipmentEventCommand, ShipmentDto>
{
    private readonly IGestorInventarioDbContext context;

    public RecordShipmentEventCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ShipmentDto> Handle(RecordShipmentEventCommand request, CancellationToken cancellationToken)
    {
        var shipment = await context.Shipments
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
            .ConfigureAwait(false);

        if (shipment is null)
        {
            throw new NotFoundException(nameof(Shipment), request.ShipmentId);
        }

        var shipmentEvent = new ShipmentEvent
        {
            Status = request.Status.Trim(),
            EventDate = request.EventDate,
            Location = request.Location?.Trim(),
            Description = request.Description?.Trim()
        };

        shipment.Events.Add(shipmentEvent);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var persisted = await context.Shipments
            .AsNoTracking()
            .Include(s => s.Warehouse)
            .Include(s => s.Lines)
                .ThenInclude(line => line.SalesOrderLine)
                    .ThenInclude(orderLine => orderLine!.Variant)
                        .ThenInclude(variant => variant!.Product)
            .Include(s => s.Events)
            .FirstAsync(s => s.Id == shipment.Id, cancellationToken)
            .ConfigureAwait(false);

        return persisted.ToDto();
    }
}
