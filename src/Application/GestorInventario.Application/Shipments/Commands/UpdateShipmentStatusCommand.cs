using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Shipments.Models;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;

namespace GestorInventario.Application.Shipments.Commands;

public record UpdateShipmentStatusCommand(
    int ShipmentId,
    ShipmentStatus Status,
    DateTime? DeliveredAt,
    DateTime? EstimatedDeliveryDate,
    string? Notes) : IRequest<ShipmentDto>;

public class UpdateShipmentStatusCommandValidator : AbstractValidator<UpdateShipmentStatusCommand>
{
    public UpdateShipmentStatusCommandValidator()
    {
        RuleFor(command => command.ShipmentId)
            .GreaterThan(0);

        RuleFor(command => command.Notes)
            .MaximumLength(200);
    }
}

public class UpdateShipmentStatusCommandHandler : IRequestHandler<UpdateShipmentStatusCommand, ShipmentDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdateShipmentStatusCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ShipmentDto> Handle(UpdateShipmentStatusCommand request, CancellationToken cancellationToken)
    {
        var shipment = await context.Shipments
            .Include(s => s.Lines)
                .ThenInclude(line => line.SalesOrderLine)
                    .ThenInclude(orderLine => orderLine!.Allocations)
                        .ThenInclude(allocation => allocation.Warehouse)
            .Include(s => s.SalesOrder)
                .ThenInclude(order => order!.Lines)
                    .ThenInclude(line => line.Allocations)
            .Include(s => s.SalesOrder)
                .ThenInclude(order => order!.Shipments)
            .Include(s => s.Warehouse)
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
            .ConfigureAwait(false);

        if (shipment is null)
        {
            throw new NotFoundException(nameof(Shipment), request.ShipmentId);
        }

        if (shipment.Status == ShipmentStatus.Cancelled || shipment.Status == ShipmentStatus.Delivered)
        {
            throw new ApplicationValidationException($"Shipment in status '{shipment.Status}' cannot be updated.");
        }

        shipment.Status = request.Status;
        shipment.Notes = request.Notes?.Trim();
        shipment.EstimatedDeliveryDate = request.EstimatedDeliveryDate;

        if (request.Status == ShipmentStatus.InTransit && shipment.ShippedAt is null)
        {
            shipment.ShippedAt = DateTime.UtcNow;
        }

        if (request.Status == ShipmentStatus.Delivered)
        {
            var deliveredAt = request.DeliveredAt ?? DateTime.UtcNow;
            shipment.DeliveredAt = deliveredAt;
            shipment.ShippedAt ??= deliveredAt;

            foreach (var line in shipment.Lines)
            {
                if (line.SalesOrderLine is null)
                {
                    continue;
                }

                var allocation = line.SalesOrderLine.Allocations.FirstOrDefault(a => a.Id == line.SalesOrderAllocationId);
                if (allocation is null)
                {
                    continue;
                }

                allocation.Status = SalesOrderAllocationStatus.Delivered;
                allocation.ReleasedAt = deliveredAt;
            }

            var order = shipment.SalesOrder;
            if (order is not null)
            {
                if (order.Lines.SelectMany(line => line.Allocations).All(allocation => allocation.Status == SalesOrderAllocationStatus.Delivered))
                {
                    order.Status = SalesOrderStatus.Delivered;
                }
            }
        }

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
