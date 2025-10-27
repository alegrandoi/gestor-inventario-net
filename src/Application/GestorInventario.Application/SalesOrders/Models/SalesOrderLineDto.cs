namespace GestorInventario.Application.SalesOrders.Models;

public record SalesOrderLineDto(
    int Id,
    int VariantId,
    decimal Quantity,
    decimal UnitPrice,
    decimal? Discount,
    int? TaxRateId,
    decimal TotalLine,
    string VariantSku,
    string ProductName,
    IReadOnlyCollection<SalesOrderAllocationDto> Allocations);
