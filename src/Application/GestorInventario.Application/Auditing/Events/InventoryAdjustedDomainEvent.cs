using System.Collections.Generic;
using GestorInventario.Domain.Enums;
using MediatR;

namespace GestorInventario.Application.Auditing.Events;

public sealed record InventoryAdjustedDomainEvent(
    int VariantId,
    string VariantSku,
    string ProductName,
    IReadOnlyCollection<InventoryAdjustmentDetail> Adjustments,
    InventoryTransactionType TransactionType,
    decimal Quantity,
    int? DestinationWarehouseId,
    string? ReferenceType,
    int? ReferenceId,
    string? Notes,
    DateTime OccurredAt) : INotification;

public sealed record InventoryAdjustmentDetail(
    int WarehouseId,
    string WarehouseName,
    decimal QuantityBefore,
    decimal QuantityAfter);
