namespace GestorInventario.Infrastructure.Messaging;

public interface IMessageBus : IAsyncDisposable
{
    Task PublishAsync(string routingKey, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);

    Task SubscribeAsync(string queueName, string routingKey, Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler, CancellationToken cancellationToken);
}
