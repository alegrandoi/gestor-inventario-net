using MediatR;

namespace GestorInventario.Application.Auditing.Events;

public sealed record UserRoleChangedDomainEvent(
    int UserId,
    string Username,
    string PreviousRole,
    string NewRole) : INotification;
