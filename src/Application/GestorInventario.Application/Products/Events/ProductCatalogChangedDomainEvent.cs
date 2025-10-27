using MediatR;

namespace GestorInventario.Application.Products.Events;

public sealed record ProductCatalogChangedDomainEvent(
    int ProductId,
    string Code,
    string Name,
    decimal DefaultPrice,
    string Currency,
    bool IsActive,
    ProductCatalogChangeType ChangeType) : INotification;
