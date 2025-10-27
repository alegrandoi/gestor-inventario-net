using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.PurchaseOrders.Models;

public static class PurchaseOrderMappingExtensions
{
    public static PurchaseOrderDto ToDto(this PurchaseOrder order)
    {
        var lines = order.Lines
            .Select(line => new PurchaseOrderLineDto(
                line.Id,
                line.VariantId,
                line.Quantity,
                line.UnitPrice,
                line.Discount,
                line.TaxRateId,
                line.TotalLine,
                line.Variant?.Sku ?? string.Empty,
                line.Variant?.Product?.Name ?? string.Empty))
            .ToList();

        return new PurchaseOrderDto(
            order.Id,
            order.SupplierId,
            order.OrderDate,
            order.Status,
            order.TotalAmount,
            order.Currency,
            order.Notes,
            lines,
            order.Supplier?.Name ?? string.Empty);
    }
}
