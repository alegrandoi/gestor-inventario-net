using GestorInventario.Application.Auditing.Events;
using GestorInventario.Domain.Enums;

namespace GestorInventario.Application.Inventory.Notifications;

public sealed record InventoryAdjustmentNotification(
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
    DateTime OccurredAt);
