using GestorInventario.Domain.Enums;
using MediatR;

namespace GestorInventario.Application.PurchaseOrders.Events;

public sealed record PurchaseOrderStatusChangedDomainEvent(
    int OrderId,
    PurchaseOrderStatus PreviousStatus,
    PurchaseOrderStatus NewStatus,
    decimal TotalAmount,
    string SupplierName,
    DateTime UpdatedAt) : INotification;
