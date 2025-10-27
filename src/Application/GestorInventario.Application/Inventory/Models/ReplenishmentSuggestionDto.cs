namespace GestorInventario.Application.Inventory.Models;

public record ReplenishmentSuggestionDto(
    int VariantId,
    string VariantSku,
    string ProductName,
    int WarehouseId,
    string WarehouseName,
    decimal OnHand,
    decimal Reserved,
    decimal RecommendedQuantity,
    decimal? SafetyStock,
    decimal? ReorderPoint,
    decimal? LeadTimeDemand,
    decimal AverageDailyDemand);
