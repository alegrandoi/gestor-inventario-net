using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.SalesOrders.Commands;
using GestorInventario.Application.SalesOrders.Services;
using GestorInventario.Application.Shipments.Models;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;

namespace GestorInventario.Application.Shipments.Commands;

public record CreateShipmentCommand(
    int SalesOrderId,
    int WarehouseId,
    int? CarrierId,
    string? TrackingNumber,
    DateTime? ShippedAt,
    DateTime? EstimatedDeliveryDate,
    decimal? TotalWeight,
    string? Notes,
    IReadOnlyCollection<CreateShipmentLineRequest> Lines) : IRequest<ShipmentDto>;

public record CreateShipmentLineRequest(int SalesOrderLineId, decimal Quantity, decimal? Weight);

public class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(command => command.SalesOrderId)
            .GreaterThan(0);

        RuleFor(command => command.WarehouseId)
            .GreaterThan(0);

        RuleFor(command => command.CarrierId)
            .GreaterThan(0)
            .When(command => command.CarrierId.HasValue);

        RuleFor(command => command.TrackingNumber)
            .MaximumLength(100);

        RuleFor(command => command.Notes)
            .MaximumLength(200);

        RuleFor(command => command.TotalWeight)
            .GreaterThanOrEqualTo(0)
            .When(command => command.TotalWeight.HasValue);

        RuleForEach(command => command.Lines)
            .ChildRules(line =>
            {
                line.RuleFor(l => l.SalesOrderLineId).GreaterThan(0);
                line.RuleFor(l => l.Quantity).GreaterThan(0);
                line.RuleFor(l => l.Weight)
                    .GreaterThanOrEqualTo(0)
                    .When(l => l.Weight.HasValue);
            });
    }
}

public class CreateShipmentCommandHandler : IRequestHandler<CreateShipmentCommand, ShipmentDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreateShipmentCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ShipmentDto> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        if (!request.Lines.Any())
        {
            throw new ApplicationValidationException("At least one shipment line is required.");
        }

        var order = await context.SalesOrders
            .Include(o => o.Carrier)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Allocations)
                    .ThenInclude(allocation => allocation.Warehouse)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Variant)
                    .ThenInclude(variant => variant!.Product)
            .FirstOrDefaultAsync(o => o.Id == request.SalesOrderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            throw new NotFoundException(nameof(SalesOrder), request.SalesOrderId);
        }

        if (order.Status == SalesOrderStatus.Cancelled)
        {
            throw new ApplicationValidationException("Cannot create a shipment for a cancelled order.");
        }

        Carrier? carrier = null;
        if (request.CarrierId.HasValue)
        {
            carrier = await context.Carriers.FindAsync([request.CarrierId.Value], cancellationToken).ConfigureAwait(false);
            if (carrier is null)
            {
                throw new NotFoundException(nameof(Carrier), request.CarrierId.Value);
            }
        }
        else if (order.Carrier is not null)
        {
            carrier = order.Carrier;
        }

        var resolvedCarrierId = request.CarrierId ?? order.CarrierId;

        var shipment = new Shipment
        {
            SalesOrderId = order.Id,
            WarehouseId = request.WarehouseId,
            CarrierId = resolvedCarrierId,
            Carrier = carrier,
            TrackingNumber = request.TrackingNumber?.Trim(),
            ShippedAt = request.ShippedAt,
            EstimatedDeliveryDate = request.EstimatedDeliveryDate,
            TotalWeight = request.TotalWeight,
            Notes = request.Notes?.Trim(),
            Status = request.ShippedAt.HasValue ? ShipmentStatus.InTransit : ShipmentStatus.Created
        };

        var shipmentAllocations = new List<SalesOrderShipmentAllocation>();

        foreach (var lineRequest in request.Lines)
        {
            var line = order.Lines.FirstOrDefault(orderLine => orderLine.Id == lineRequest.SalesOrderLineId);
            if (line is null)
            {
                throw new ApplicationValidationException($"Sales order line {lineRequest.SalesOrderLineId} does not belong to order {order.Id}.");
            }

            var allocation = line.Allocations.FirstOrDefault(a => a.WarehouseId == request.WarehouseId);
            if (allocation is null)
            {
                throw new ApplicationValidationException($"No reserved allocation for line {line.Id} in warehouse {request.WarehouseId}.");
            }

            var remaining = allocation.Quantity - allocation.FulfilledQuantity;
            if (remaining < lineRequest.Quantity)
            {
                throw new ApplicationValidationException($"Requested quantity {lineRequest.Quantity} exceeds reserved {remaining} for line {line.Id}.");
            }

            shipmentAllocations.Add(new SalesOrderShipmentAllocation(line.VariantId, request.WarehouseId, lineRequest.Quantity));

            shipment.Lines.Add(new ShipmentLine
            {
                SalesOrderLineId = line.Id,
                SalesOrderAllocationId = allocation.Id,
                Quantity = lineRequest.Quantity,
                Weight = lineRequest.Weight
            });
        }

        var shipmentProcessor = new SalesOrderShipmentProcessor(context);
        await shipmentProcessor.ProcessAsync(order, shipmentAllocations, SalesOrderStatus.Shipped, cancellationToken).ConfigureAwait(false);

        if (shipment.Status == ShipmentStatus.Created && request.ShippedAt.HasValue)
        {
            shipment.Status = ShipmentStatus.InTransit;
        }

        context.Shipments.Add(shipment);

        var allAllocations = order.Lines.SelectMany(line => line.Allocations).ToList();
        if (allAllocations.All(allocation => allocation.FulfilledQuantity >= allocation.Quantity))
        {
            order.Status = SalesOrderStatus.Shipped;
        }
        else if (order.Status == SalesOrderStatus.Pending)
        {
            order.Status = SalesOrderStatus.Confirmed;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var persisted = await context.Shipments
            .AsNoTracking()
            .Include(s => s.Warehouse)
            .Include(s => s.Carrier)
            .Include(s => s.Lines)
                .ThenInclude(line => line.SalesOrderLine)
                    .ThenInclude(orderLine => orderLine!.Variant)
                        .ThenInclude(variant => variant!.Product)
            .Include(s => s.Events)
            .FirstAsync(entity => entity.Id == shipment.Id, cancellationToken)
            .ConfigureAwait(false);

        return persisted.ToDto();
    }
}
