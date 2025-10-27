using System.Collections.Generic;
using System.Linq;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.SalesOrders.Commands;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.SalesOrders.Services;

public class SalesOrderShipmentProcessor
{
    private readonly IGestorInventarioDbContext context;

    public SalesOrderShipmentProcessor(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<SalesOrderShipmentInventoryAdjustment>> ProcessAsync(
        SalesOrder order,
        IReadOnlyCollection<SalesOrderShipmentAllocation> allocations,
        SalesOrderStatus targetStatus,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var groupedAllocations = allocations
            .GroupBy(allocation => allocation.VariantId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var shipmentBuilders = allocations.Any()
            ? new Dictionary<int, ShipmentBuildState>()
            : null;

        var stockCache = new Dictionary<(int VariantId, int WarehouseId), InventoryStock>();
        var adjustments = new Dictionary<int, SalesOrderShipmentAdjustmentAccumulator>();

        foreach (var line in order.Lines)
        {
            if (!groupedAllocations.TryGetValue(line.VariantId, out var lineAllocations))
            {
                continue;
            }

            var lineRemaining = line.Quantity - line.Allocations.Sum(existing => existing.FulfilledQuantity);
            var requestedQuantity = lineAllocations.Sum(allocation => allocation.Quantity);

            if (requestedQuantity > lineRemaining)
            {
                throw new ApplicationValidationException($"Allocation quantity exceeds remaining shippable units for variant {line.VariantId}. Requested {requestedQuantity} and only {lineRemaining} pending.");
            }

            foreach (var allocationRequest in lineAllocations)
            {
                var allocationEntity = line.Allocations.FirstOrDefault(a => a.WarehouseId == allocationRequest.WarehouseId);
                if (allocationEntity is null)
                {
                    throw new ApplicationValidationException($"No reserved allocation for variant {line.VariantId} in warehouse {allocationRequest.WarehouseId}.");
                }

                var allocationRemaining = allocationEntity.Quantity - allocationEntity.FulfilledQuantity;
                if (allocationRemaining <= 0)
                {
                    throw new ApplicationValidationException($"Allocation for variant {line.VariantId} in warehouse {allocationRequest.WarehouseId} is already fulfilled.");
                }

                if (allocationRemaining < allocationRequest.Quantity)
                {
                    throw new ApplicationValidationException($"Shipment quantity {allocationRequest.Quantity} exceeds reserved amount {allocationRemaining} for variant {line.VariantId} in warehouse {allocationRequest.WarehouseId}.");
                }

                var stockKey = (line.VariantId, allocationRequest.WarehouseId);
                if (!stockCache.TryGetValue(stockKey, out var stock))
                {
                    stock = await context.InventoryStocks
                        .FirstOrDefaultAsync(s => s.VariantId == line.VariantId && s.WarehouseId == allocationRequest.WarehouseId, cancellationToken)
                        .ConfigureAwait(false)
                        ?? throw new ApplicationValidationException($"No stock found for variant {line.VariantId} in warehouse {allocationRequest.WarehouseId}.");

                    stockCache[stockKey] = stock;
                }

                if (stock.Quantity < allocationRequest.Quantity)
                {
                    throw new ApplicationValidationException($"Insufficient stock for variant {line.VariantId} in warehouse {allocationRequest.WarehouseId}.");
                }

                var warehouse = allocationEntity.Warehouse
                    ?? throw new ApplicationValidationException($"Warehouse {allocationRequest.WarehouseId} not loaded for allocation {allocationEntity.Id}.");

                var quantityBefore = stock.Quantity;
                stock.Quantity -= allocationRequest.Quantity;
                stock.ReservedQuantity = Math.Max(0, stock.ReservedQuantity - allocationRequest.Quantity);
                var quantityAfter = stock.Quantity;

                allocationEntity.FulfilledQuantity += allocationRequest.Quantity;
                allocationEntity.ShippedAt ??= now;
                allocationEntity.Status = allocationEntity.FulfilledQuantity >= allocationEntity.Quantity
                    ? SalesOrderAllocationStatus.Shipped
                    : SalesOrderAllocationStatus.PartiallyShipped;

                context.InventoryTransactions.Add(new InventoryTransaction
                {
                    VariantId = line.VariantId,
                    WarehouseId = allocationRequest.WarehouseId,
                    TransactionType = InventoryTransactionType.Out,
                    Quantity = allocationRequest.Quantity,
                    TransactionDate = now,
                    ReferenceType = nameof(SalesOrder),
                    ReferenceId = order.Id,
                    Notes = "Sales order shipment"
                });

                var variant = line.Variant
                    ?? throw new ApplicationValidationException($"Variant {line.VariantId} not loaded for shipment processing.");

                if (!adjustments.TryGetValue(variant.Id, out var accumulator))
                {
                    accumulator = new SalesOrderShipmentAdjustmentAccumulator(variant, now);
                    adjustments[variant.Id] = accumulator;
                }

                accumulator.Register(warehouse, quantityBefore, quantityAfter, allocationRequest.Quantity);

                if (shipmentBuilders is not null)
                {
                    var builder = GetOrCreateShipmentBuilder(
                        shipmentBuilders,
                        order,
                        warehouse,
                        now,
                        targetStatus);

                    builder.AddLine(line, allocationEntity, allocationRequest.Quantity);
                }
            }
        }

        if (targetStatus == SalesOrderStatus.Delivered)
        {
            foreach (var allocation in order.Lines.SelectMany(line => line.Allocations))
            {
                if (allocation.FulfilledQuantity < allocation.Quantity)
                {
                    throw new ApplicationValidationException("Cannot mark order as delivered with pending quantities.");
                }

                allocation.Status = SalesOrderAllocationStatus.Delivered;
                allocation.ReleasedAt = now;
            }

            foreach (var shipment in order.Shipments)
            {
                shipment.Status = ShipmentStatus.Delivered;
                shipment.DeliveredAt ??= now;
                shipment.ShippedAt ??= shipment.DeliveredAt ?? now;
                shipment.EstimatedDeliveryDate ??= shipment.DeliveredAt;
            }
        }

        if (shipmentBuilders is not null)
        {
            foreach (var builder in shipmentBuilders.Values)
            {
                builder.FinalizeShipment();
            }
        }

        return adjustments.Values
            .Select(accumulator => accumulator.Build())
            .ToList();
    }

    private static ShipmentBuildState GetOrCreateShipmentBuilder(
        IDictionary<int, ShipmentBuildState> registry,
        SalesOrder order,
        Warehouse warehouse,
        DateTime now,
        SalesOrderStatus targetStatus)
    {
        if (registry.TryGetValue(warehouse.Id, out var existing))
        {
            return existing;
        }

        var shipmentStatus = targetStatus == SalesOrderStatus.Delivered
            ? ShipmentStatus.Delivered
            : ShipmentStatus.InTransit;

        var shipment = new Shipment
        {
            SalesOrder = order,
            SalesOrderId = order.Id,
            Warehouse = warehouse,
            WarehouseId = warehouse.Id,
            CarrierId = order.CarrierId,
            Carrier = order.Carrier,
            Status = shipmentStatus,
            ShippedAt = now,
            DeliveredAt = shipmentStatus == ShipmentStatus.Delivered ? now : null,
            EstimatedDeliveryDate = shipmentStatus == ShipmentStatus.Delivered
                ? now
                : CalculateEstimatedDelivery(order, now),
            Notes = order.Notes
        };

        order.Shipments.Add(shipment);
        var builder = new ShipmentBuildState(shipment);
        registry[warehouse.Id] = builder;
        return builder;
    }

    private static DateTime CalculateEstimatedDelivery(SalesOrder order, DateTime shippedAt)
    {
        var leadTimes = order.Lines
            .Select(line => line.Variant?.Product?.LeadTimeDays)
            .Where(days => days.HasValue && days.Value > 0)
            .Select(days => days!.Value)
            .ToList();

        var leadTime = leadTimes.Count > 0 ? leadTimes.Max() : 3;
        return shippedAt.AddDays(leadTime);
    }

    public async Task ReleasePendingAllocationsAsync(SalesOrder order, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var stockCache = new Dictionary<(int VariantId, int WarehouseId), InventoryStock?>();

        foreach (var line in order.Lines)
        {
            foreach (var allocation in line.Allocations)
            {
                var remaining = allocation.Quantity - allocation.FulfilledQuantity;
                if (remaining <= 0)
                {
                    continue;
                }

                var key = (line.VariantId, allocation.WarehouseId);
                if (!stockCache.TryGetValue(key, out var stock))
                {
                    stock = await context.InventoryStocks
                        .FirstOrDefaultAsync(s => s.VariantId == key.VariantId && s.WarehouseId == key.WarehouseId, cancellationToken)
                        .ConfigureAwait(false);

                    stockCache[key] = stock;
                }

                if (stock is not null)
                {
                    stock.ReservedQuantity = Math.Max(0, stock.ReservedQuantity - remaining);
                }

                allocation.Status = SalesOrderAllocationStatus.Released;
                allocation.ReleasedAt = now;
            }
        }
    }
}

internal sealed class ShipmentBuildState
{
    private readonly Shipment shipment;
    private decimal totalWeight;

    public ShipmentBuildState(Shipment shipment)
    {
        this.shipment = shipment;
    }

    public void AddLine(SalesOrderLine line, SalesOrderAllocation allocation, decimal quantity)
    {
        var variant = line.Variant
            ?? throw new ApplicationValidationException($"Variant {line.VariantId} not loaded for shipment processing.");

        decimal? weight = null;
        if (variant.Product?.WeightKg is decimal weightKg && weightKg > 0)
        {
            weight = Math.Round(weightKg * quantity, 4);
            totalWeight += weight.Value;
        }

        shipment.Lines.Add(new ShipmentLine
        {
            SalesOrderLine = line,
            SalesOrderLineId = line.Id,
            SalesOrderAllocation = allocation,
            SalesOrderAllocationId = allocation.Id,
            Quantity = quantity,
            Weight = weight
        });
    }

    public void FinalizeShipment()
    {
        shipment.TotalWeight = totalWeight > 0 ? Math.Round(totalWeight, 4) : null;
    }
}

public sealed record SalesOrderShipmentInventoryAdjustment(
    ProductVariant Variant,
    IReadOnlyCollection<SalesOrderShipmentAdjustmentDetail> Adjustments,
    decimal Quantity,
    DateTime OccurredAt);

public sealed record SalesOrderShipmentAdjustmentDetail(
    Warehouse Warehouse,
    decimal QuantityBefore,
    decimal QuantityAfter);

internal sealed class SalesOrderShipmentAdjustmentAccumulator
{
    private readonly Dictionary<int, SalesOrderShipmentAdjustmentDetail> adjustments = new();

    public SalesOrderShipmentAdjustmentAccumulator(ProductVariant variant, DateTime occurredAt)
    {
        Variant = variant;
        OccurredAt = occurredAt;
    }

    public ProductVariant Variant { get; }

    public DateTime OccurredAt { get; }

    public decimal Quantity { get; private set; }

    public void Register(Warehouse warehouse, decimal quantityBefore, decimal quantityAfter, decimal shippedQuantity)
    {
        Quantity += shippedQuantity;

        if (!adjustments.TryGetValue(warehouse.Id, out var detail))
        {
            adjustments[warehouse.Id] = new SalesOrderShipmentAdjustmentDetail(warehouse, quantityBefore, quantityAfter);
            return;
        }

        adjustments[warehouse.Id] = detail with { QuantityAfter = quantityAfter };
    }

    public SalesOrderShipmentInventoryAdjustment Build()
    {
        return new SalesOrderShipmentInventoryAdjustment(
            Variant,
            adjustments.Values.ToList(),
            Quantity,
            OccurredAt);
    }
}
