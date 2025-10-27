using GestorInventario.Application.PurchaseOrders.Models;
using GestorInventario.Domain.Enums;

namespace GestorInventario.Application.PurchaseOrders.Models;

public record PurchaseOrderDto(
    int Id,
    int SupplierId,
    DateTime OrderDate,
    PurchaseOrderStatus Status,
    decimal TotalAmount,
    string Currency,
    string? Notes,
    IReadOnlyCollection<PurchaseOrderLineDto> Lines,
    string SupplierName);
