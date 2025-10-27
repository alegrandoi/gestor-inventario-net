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
using GestorInventario.Application.PurchaseOrders.Events;
using GestorInventario.Application.PurchaseOrders.Models;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.PurchaseOrders.Commands;

public record UpdatePurchaseOrderStatusCommand(int OrderId, PurchaseOrderStatus Status, int? WarehouseId) : IRequest<PurchaseOrderDto>;

public class UpdatePurchaseOrderStatusCommandValidator : AbstractValidator<UpdatePurchaseOrderStatusCommand>
{
    public UpdatePurchaseOrderStatusCommandValidator()
    {
        RuleFor(command => command.OrderId)
            .GreaterThan(0);

        RuleFor(command => command.WarehouseId)
            .GreaterThan(0)
            .When(command => command.Status == PurchaseOrderStatus.Received);
    }
}

public class UpdatePurchaseOrderStatusCommandHandler : IRequestHandler<UpdatePurchaseOrderStatusCommand, PurchaseOrderDto>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IPublisher publisher;

    public UpdatePurchaseOrderStatusCommandHandler(IGestorInventarioDbContext context, IPublisher publisher)
    {
        this.context = context;
        this.publisher = publisher;
    }

    public async Task<PurchaseOrderDto> Handle(UpdatePurchaseOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await context.PurchaseOrders
            .Include(o => o.Lines)
                .ThenInclude(line => line.Variant)
                    .ThenInclude(variant => variant!.Product)
            .Include(o => o.Supplier)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            throw new NotFoundException(nameof(PurchaseOrder), request.OrderId);
        }

        var currentStatus = NormalizeStatus(order.Status);

        if (currentStatus is PurchaseOrderStatus.Cancelled or PurchaseOrderStatus.Received)
        {
            throw new ApplicationValidationException($"Purchase order in status '{order.Status}' cannot be updated.");
        }

        ValidateStatusTransition(currentStatus, request.Status);

        var previousStatus = currentStatus;

        IReadOnlyCollection<PurchaseOrderInventoryAdjustment> stockAdjustments = Array.Empty<PurchaseOrderInventoryAdjustment>();

        if (request.Status == PurchaseOrderStatus.Received)
        {
            if (!request.WarehouseId.HasValue)
            {
                throw new ApplicationValidationException("Warehouse is required when receiving a purchase order.");
            }

            var warehouse = await context.Warehouses.FindAsync([request.WarehouseId.Value], cancellationToken).ConfigureAwait(false);
            if (warehouse is null)
            {
                throw new NotFoundException(nameof(Warehouse), request.WarehouseId.Value);
            }

            var adjustments = new Dictionary<int, PurchaseOrderInventoryAdjustment>();
            var occurredAt = DateTime.UtcNow;

            foreach (var line in order.Lines)
            {
                var variant = line.Variant
                    ?? throw new ApplicationValidationException($"Variant {line.VariantId} is not available for order {order.Id}.");

                var detail = await AddStockAsync(line, warehouse, order.Id, occurredAt, cancellationToken).ConfigureAwait(false);

                if (!adjustments.TryGetValue(variant.Id, out var accumulator))
                {
                    accumulator = new PurchaseOrderInventoryAdjustment(variant, occurredAt);
                    adjustments[variant.Id] = accumulator;
                }

                accumulator.Register(detail, line.Quantity);
            }

            stockAdjustments = adjustments.Values.ToList();
        }

        order.Status = request.Status;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var adjustment in stockAdjustments)
        {
            await publisher.Publish(
                    new InventoryAdjustedDomainEvent(
                        adjustment.Variant.Id,
                        adjustment.Variant.Sku,
                        adjustment.Variant.Product?.Name ?? string.Empty,
                        adjustment.Details,
                        InventoryTransactionType.In,
                        adjustment.Quantity,
                        null,
                        nameof(PurchaseOrder),
                        order.Id,
                        "Purchase order reception",
                        adjustment.OccurredAt),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        order = await context.PurchaseOrders
            .AsNoTracking()
            .Include(o => o.Supplier)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Variant)
                    .ThenInclude(variant => variant!.Product)
            .FirstAsync(o => o.Id == order.Id, cancellationToken)
            .ConfigureAwait(false);

        var orderDto = order.ToDto();

        if (previousStatus != orderDto.Status)
        {
            await publisher.Publish(
                new PurchaseOrderStatusChangedDomainEvent(
                    orderDto.Id,
                    previousStatus,
                    orderDto.Status,
                    orderDto.TotalAmount,
                    orderDto.SupplierName,
                    DateTime.UtcNow),
                cancellationToken).ConfigureAwait(false);
        }

        return orderDto;
    }

    private static PurchaseOrderStatus NormalizeStatus(PurchaseOrderStatus status)
    {
        return Enum.IsDefined(typeof(PurchaseOrderStatus), status)
            ? status
            : PurchaseOrderStatus.Pending;
    }

    private static void ValidateStatusTransition(PurchaseOrderStatus currentStatus, PurchaseOrderStatus targetStatus)
    {
        if (currentStatus == targetStatus)
        {
            throw new ApplicationValidationException($"Purchase order is already in status '{targetStatus}'.");
        }

        switch (targetStatus)
        {
            case PurchaseOrderStatus.Pending:
                throw new ApplicationValidationException("Purchase order cannot revert to pending status.");
            case PurchaseOrderStatus.Ordered:
                if (currentStatus != PurchaseOrderStatus.Pending)
                {
                    throw new ApplicationValidationException($"Purchase order in status '{currentStatus}' cannot move to '{targetStatus}'.");
                }

                break;
            case PurchaseOrderStatus.Received:
                if (currentStatus is not (PurchaseOrderStatus.Pending or PurchaseOrderStatus.Ordered))
                {
                    throw new ApplicationValidationException($"Purchase order in status '{currentStatus}' cannot move to '{targetStatus}'.");
                }

                break;
            case PurchaseOrderStatus.Cancelled:
                if (currentStatus is not (PurchaseOrderStatus.Pending or PurchaseOrderStatus.Ordered))
                {
                    throw new ApplicationValidationException($"Purchase order in status '{currentStatus}' cannot move to '{targetStatus}'.");
                }

                break;
            default:
                throw new ApplicationValidationException($"Unsupported purchase order status '{targetStatus}'.");
        }
    }

    private async Task<InventoryAdjustmentDetail> AddStockAsync(
        PurchaseOrderLine line,
        Warehouse warehouse,
        int orderId,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var stock = await context.InventoryStocks
            .FirstOrDefaultAsync(s => s.VariantId == line.VariantId && s.WarehouseId == warehouse.Id, cancellationToken)
            .ConfigureAwait(false);

        if (stock is null)
        {
            stock = new InventoryStock
            {
                VariantId = line.VariantId,
                WarehouseId = warehouse.Id,
                Quantity = 0,
                ReservedQuantity = 0,
                MinStockLevel = 0
            };
            context.InventoryStocks.Add(stock);
        }

        var quantityBefore = stock.Quantity;
        stock.Quantity += line.Quantity;

        context.InventoryTransactions.Add(new InventoryTransaction
        {
            VariantId = line.VariantId,
            WarehouseId = warehouse.Id,
            TransactionType = InventoryTransactionType.In,
            Quantity = line.Quantity,
            TransactionDate = occurredAt,
            ReferenceType = nameof(PurchaseOrder),
            ReferenceId = orderId,
            Notes = "Purchase order reception"
        });

        return new InventoryAdjustmentDetail(
            warehouse.Id,
            warehouse.Name,
            quantityBefore,
            stock.Quantity);
    }
}

internal sealed class PurchaseOrderInventoryAdjustment
{
    private readonly List<InventoryAdjustmentDetail> details = new();

    public PurchaseOrderInventoryAdjustment(ProductVariant variant, DateTime occurredAt)
    {
        Variant = variant;
        OccurredAt = occurredAt;
    }

    public ProductVariant Variant { get; }

    public DateTime OccurredAt { get; }

    public decimal Quantity { get; private set; }

    public IReadOnlyCollection<InventoryAdjustmentDetail> Details => details;

    public void Register(InventoryAdjustmentDetail detail, decimal addedQuantity)
    {
        Quantity += addedQuantity;

        var existingIndex = details.FindIndex(existing => existing.WarehouseId == detail.WarehouseId);
        if (existingIndex >= 0)
        {
            var existing = details[existingIndex];
            details[existingIndex] = new InventoryAdjustmentDetail(
                existing.WarehouseId,
                existing.WarehouseName,
                existing.QuantityBefore,
                detail.QuantityAfter);
            return;
        }

        details.Add(detail);
    }
}
