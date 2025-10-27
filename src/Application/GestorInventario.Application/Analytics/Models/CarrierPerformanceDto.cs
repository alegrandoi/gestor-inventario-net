namespace GestorInventario.Application.Analytics.Models;

public record CarrierPerformanceDto(
    int? CarrierId,
    string CarrierName,
    int TotalShipments,
    int InTransitShipments,
    int DeliveredShipments,
    double OnTimeRate,
    double AverageDelayDays);
