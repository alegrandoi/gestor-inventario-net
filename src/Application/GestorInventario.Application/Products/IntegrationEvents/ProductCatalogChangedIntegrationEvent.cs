using GestorInventario.Application.Common.Messaging;
using GestorInventario.Application.Products.Events;

namespace GestorInventario.Application.Products.IntegrationEvents;

public sealed record ProductCatalogChangedIntegrationEvent(
    int ProductId,
    string Code,
    string Name,
    decimal DefaultPrice,
    string Currency,
    bool IsActive,
    ProductCatalogChangeType ChangeType) : IIntegrationEvent
{
    public string EventName => "product.catalog.changed";
}
