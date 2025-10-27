using MediatR;

namespace GestorInventario.Application.Auditing.Events;

public sealed record ProductDeletedDomainEvent(
    int ProductId,
    string Code,
    string Name,
    decimal DefaultPrice,
    string Currency,
    bool WasActive) : INotification;
