namespace GestorInventario.Application.Analytics.Models;

public record DemandForecastDto(
    int VariantId,
    string VariantSku,
    string ProductName,
    IReadOnlyCollection<DemandPointDto> Historical,
    IReadOnlyCollection<DemandPointDto> Forecast
);

public record DemandPointDto(
    DateOnly Period,
    decimal Quantity
);
