using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Exceptions;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.SalesOrders.Events;
using GestorInventario.Application.SalesOrders.Models;
using GestorInventario.Application.SalesOrders.Services;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.SalesOrders.Commands;

public record UpdateSalesOrderStatusCommand(
    int OrderId,
    SalesOrderStatus Status,
    IReadOnlyCollection<SalesOrderShipmentAllocation>? Allocations) : IRequest<SalesOrderDto>;

public record SalesOrderShipmentAllocation(int VariantId, int WarehouseId, decimal Quantity);

public class UpdateSalesOrderStatusCommandValidator : AbstractValidator<UpdateSalesOrderStatusCommand>
{
    public UpdateSalesOrderStatusCommandValidator()
    {
        RuleFor(command => command.OrderId)
            .GreaterThan(0);

        RuleForEach(command => command.Allocations)
            .ChildRules(allocation =>
            {
                allocation.RuleFor(a => a.VariantId).GreaterThan(0);
                allocation.RuleFor(a => a.WarehouseId).GreaterThan(0);
                allocation.RuleFor(a => a.Quantity).GreaterThan(0);
            });
    }
}

public class UpdateSalesOrderStatusCommandHandler : IRequestHandler<UpdateSalesOrderStatusCommand, SalesOrderDto>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IPublisher publisher;

    public UpdateSalesOrderStatusCommandHandler(IGestorInventarioDbContext context, IPublisher publisher)
    {
        this.context = context;
        this.publisher = publisher;
    }

    public async Task<SalesOrderDto> Handle(UpdateSalesOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await context.SalesOrders
            .Include(o => o.Lines)
                .ThenInclude(line => line.Allocations)
                    .ThenInclude(allocation => allocation.Warehouse)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Variant)
                    .ThenInclude(variant => variant!.Product)
            .Include(o => o.Customer)
            .Include(o => o.Carrier)
            .Include(o => o.ShippingRate)
            .Include(o => o.Shipments)
                .ThenInclude(shipment => shipment.Carrier)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            throw new NotFoundException(nameof(SalesOrder), request.OrderId);
        }

        var currentStatus = NormalizeStatus(order.Status);
        if (order.Status != currentStatus)
        {
            order.Status = currentStatus;
        }

        if (currentStatus is SalesOrderStatus.Cancelled or SalesOrderStatus.Delivered)
        {
            throw new ApplicationValidationException($"Sales order in status '{order.Status}' cannot be updated.");
        }

        var allocations = request.Allocations ?? Array.Empty<SalesOrderShipmentAllocation>();

        ValidateStatusTransition(currentStatus, request.Status, allocations);

        var previousStatus = currentStatus;

        var shipmentProcessor = new SalesOrderShipmentProcessor(context);

        IReadOnlyCollection<SalesOrderShipmentInventoryAdjustment> shipmentAdjustments = Array.Empty<SalesOrderShipmentInventoryAdjustment>();

        if (request.Status is SalesOrderStatus.Shipped or SalesOrderStatus.Delivered)
        {
            if (!allocations.Any() && request.Status == SalesOrderStatus.Shipped)
            {
                throw new ApplicationValidationException("Allocations are required when shipping an order.");
            }


            if (!allocations.Any() && request.Status == SalesOrderStatus.Delivered && currentStatus != SalesOrderStatus.Shipped)
            {
                throw new ApplicationValidationException("Sales order must be shipped before it can be marked as delivered.");
            }

            shipmentAdjustments = await shipmentProcessor
                .ProcessAsync(order, allocations, request.Status, cancellationToken)
                .ConfigureAwait(false);
        }

        if (request.Status == SalesOrderStatus.Cancelled)
        {
            await shipmentProcessor.ReleasePendingAllocationsAsync(order, cancellationToken).ConfigureAwait(false);
        }

        order.Status = request.Status;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var adjustment in shipmentAdjustments)
        {
            var variant = adjustment.Variant;
            var details = adjustment.Adjustments
                .Select(detail => new InventoryAdjustmentDetail(
                    detail.Warehouse.Id,
                    detail.Warehouse.Name,
                    detail.QuantityBefore,
                    detail.QuantityAfter))
                .ToList();

            await publisher.Publish(
                    new InventoryAdjustedDomainEvent(
                        variant.Id,
                        variant.Sku,
                        variant.Product?.Name ?? string.Empty,
                        details,
                        InventoryTransactionType.Out,
                        adjustment.Quantity,
                        null,
                        nameof(SalesOrder),
                        order.Id,
                        "Sales order shipment",
                        adjustment.OccurredAt),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        order = await context.SalesOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Carrier)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Variant)
                    .ThenInclude(variant => variant!.Product)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Allocations)
                    .ThenInclude(allocation => allocation.Warehouse)
            .Include(o => o.Shipments)
                .ThenInclude(shipment => shipment.Warehouse)
            .Include(o => o.Shipments)
                .ThenInclude(shipment => shipment.Carrier)
            .FirstAsync(o => o.Id == order.Id, cancellationToken)
            .ConfigureAwait(false);

        var orderDto = order.ToDto();

        if (previousStatus != orderDto.Status)
        {
            await publisher.Publish(
                new SalesOrderStatusChangedDomainEvent(
                    orderDto.Id,
                    previousStatus,
                    orderDto.Status,
                    orderDto.TotalAmount,
                    orderDto.CustomerName,
                    DateTime.UtcNow),
                cancellationToken).ConfigureAwait(false);
        }

        return orderDto;
    }

    private static void ValidateStatusTransition(
        SalesOrderStatus currentStatus,
        SalesOrderStatus targetStatus,
        IReadOnlyCollection<SalesOrderShipmentAllocation> allocations)
    {
        if (currentStatus == targetStatus)
        {
            if ((targetStatus == SalesOrderStatus.Shipped || targetStatus == SalesOrderStatus.Delivered) && allocations.Any())
            {
                return;
            }

            throw new ApplicationValidationException($"Sales order is already in status '{targetStatus}'.");
        }

        switch (targetStatus)
        {
            case SalesOrderStatus.Pending:
                throw new ApplicationValidationException("Sales order cannot revert to pending status.");
            case SalesOrderStatus.Confirmed:
                if (currentStatus != SalesOrderStatus.Pending)
                {
                    throw new ApplicationValidationException($"Sales order in status '{currentStatus}' cannot move to '{targetStatus}'.");
                }

                break;
            case SalesOrderStatus.Shipped:
                if (currentStatus is not (SalesOrderStatus.Pending or SalesOrderStatus.Confirmed or SalesOrderStatus.Shipped))
                {
                    throw new ApplicationValidationException($"Sales order in status '{currentStatus}' cannot move to '{targetStatus}'.");
                }

                break;
            case SalesOrderStatus.Delivered:
                if (currentStatus is not (SalesOrderStatus.Confirmed or SalesOrderStatus.Shipped))
                {
                    throw new ApplicationValidationException($"Sales order in status '{currentStatus}' cannot move to '{targetStatus}'.");
                }

                break;
            case SalesOrderStatus.Cancelled:
                if (currentStatus == SalesOrderStatus.Shipped)
                {
                    throw new ApplicationValidationException("Sales order cannot be cancelled after it has been shipped.");
                }

                break;
            default:
                throw new ApplicationValidationException($"Unsupported sales order status '{targetStatus}'.");
        }
    }

    private static SalesOrderStatus NormalizeStatus(SalesOrderStatus status)
    {
        return Enum.IsDefined(typeof(SalesOrderStatus), status)
            ? status
            : SalesOrderStatus.Pending;
    }
}
