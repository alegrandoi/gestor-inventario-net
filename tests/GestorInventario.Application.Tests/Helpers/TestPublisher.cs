using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace GestorInventario.Application.Tests.Helpers;

public sealed class TestPublisher : IPublisher
{
    private readonly Dictionary<Type, List<Func<INotification, CancellationToken, Task>>> handlers = new();

    public void RegisterHandler<TNotification>(Func<TNotification, CancellationToken, Task> handler)
        where TNotification : INotification
    {
        var notificationType = typeof(TNotification);
        if (!handlers.TryGetValue(notificationType, out var registeredHandlers))
        {
            registeredHandlers = new List<Func<INotification, CancellationToken, Task>>();
            handlers[notificationType] = registeredHandlers;
        }

        registeredHandlers.Add((notification, token) => handler((TNotification)notification, token));
    }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        if (notification is not INotification typedNotification)
        {
            return Task.CompletedTask;
        }

        if (!handlers.TryGetValue(notification.GetType(), out var registeredHandlers) || registeredHandlers.Count == 0)
        {
            return Task.CompletedTask;
        }

        return InvokeHandlersAsync(typedNotification, registeredHandlers, cancellationToken);
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        return Publish((object)notification!, cancellationToken);
    }

    private static async Task InvokeHandlersAsync(
        INotification notification,
        IEnumerable<Func<INotification, CancellationToken, Task>> registeredHandlers,
        CancellationToken cancellationToken)
    {
        foreach (var handler in registeredHandlers)
        {
            await handler(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
