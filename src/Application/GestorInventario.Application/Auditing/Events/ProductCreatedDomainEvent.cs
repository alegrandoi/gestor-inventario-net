using MediatR;

namespace GestorInventario.Application.Auditing.Events;

public sealed record ProductCreatedDomainEvent(
    int ProductId,
    string Code,
    string Name,
    decimal DefaultPrice,
    string Currency,
    bool IsActive) : INotification;
