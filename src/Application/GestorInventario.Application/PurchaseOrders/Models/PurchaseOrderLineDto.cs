namespace GestorInventario.Application.PurchaseOrders.Models;

public record PurchaseOrderLineDto(
    int Id,
    int VariantId,
    decimal Quantity,
    decimal UnitPrice,
    decimal? Discount,
    int? TaxRateId,
    decimal TotalLine,
    string VariantSku,
    string ProductName);
