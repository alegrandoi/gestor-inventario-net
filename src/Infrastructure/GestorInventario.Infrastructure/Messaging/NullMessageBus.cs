namespace GestorInventario.Infrastructure.Messaging;

public sealed class NullMessageBus : IMessageBus
{
    public Task PublishAsync(string routingKey, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SubscribeAsync(string queueName, string routingKey, Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler, CancellationToken cancellationToken) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
