using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Messaging;
using GestorInventario.Domain.Enums;

namespace GestorInventario.Application.Inventory.IntegrationEvents;

public sealed record InventoryAdjustedIntegrationEvent(
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
    DateTime OccurredAt) : IIntegrationEvent
{
    public string EventName => "inventory.adjusted";
}
