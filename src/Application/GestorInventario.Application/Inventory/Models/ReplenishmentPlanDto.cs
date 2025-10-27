namespace GestorInventario.Application.Inventory.Models;

public record ReplenishmentPlanDto(
    DateTime GeneratedAt,
    IReadOnlyCollection<ReplenishmentSuggestionDto> Suggestions);
