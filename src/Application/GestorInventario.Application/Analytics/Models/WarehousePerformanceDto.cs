namespace GestorInventario.Application.Analytics.Models;

public record WarehousePerformanceDto(
    int WarehouseId,
    string WarehouseName,
    int TotalShipments,
    int OnTimeShipments,
    int DelayedShipments,
    double AverageTransitDays);
