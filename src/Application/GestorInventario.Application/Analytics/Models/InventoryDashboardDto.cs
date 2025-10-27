namespace GestorInventario.Application.Analytics.Models;

public record InventoryDashboardDto(
    int TotalProducts,
    int ActiveProducts,
    decimal TotalInventoryValue,
    int LowStockVariants,
    IReadOnlyCollection<ReorderAlertDto> ReorderAlerts,
    IReadOnlyCollection<TopSellingProductDto> TopSellingProducts,
    IReadOnlyCollection<SalesTrendPointDto> MonthlySales
);

public record ReorderAlertDto(
    int VariantId,
    string ProductName,
    string VariantSku,
    decimal Quantity,
    decimal MinStockLevel,
    string Warehouse
);

public record TopSellingProductDto(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal Revenue
);

public record SalesTrendPointDto(
    int Year,
    int Month,
    decimal TotalAmount
);
