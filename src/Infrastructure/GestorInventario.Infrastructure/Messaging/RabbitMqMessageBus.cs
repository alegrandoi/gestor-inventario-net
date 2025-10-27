using GestorInventario.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GestorInventario.Infrastructure.Messaging;

public sealed class RabbitMqMessageBus : IMessageBus
{
    private readonly RabbitMqOptions options;
    private readonly ILogger<RabbitMqMessageBus> logger;
    private readonly ConnectionFactory factory;
    private IConnection? connection;
    private IModel? channel;

    public RabbitMqMessageBus(IOptions<RabbitMqOptions> options, ILogger<RabbitMqMessageBus> logger)
    {
        this.options = options.Value;
        this.logger = logger;

        factory = BuildFactory(this.options);
    }

    public async Task PublishAsync(string routingKey, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (!EnsureChannel())
        {
            logger.LogWarning("RabbitMQ channel is not available. Dropping message for {RoutingKey}.", routingKey);
            return;
        }

        var localChannel = channel;
        if (localChannel is null)
        {
            logger.LogWarning("RabbitMQ channel could not be initialized before publishing message {RoutingKey}.", routingKey);
            return;
        }

        await Task.Run(() =>
        {
            localChannel.BasicPublish(
                exchange: options.Exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: null,
                body: payload);
        }, cancellationToken).ConfigureAwait(false);
    }

    public Task SubscribeAsync(
        string queueName,
        string routingKey,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        if (!EnsureChannel())
        {
            logger.LogWarning("RabbitMQ channel is not available. Subscription to {Queue} could not be established.", queueName);
            return Task.CompletedTask;
        }

        var localChannel = channel;
        if (localChannel is null)
        {
            logger.LogWarning("RabbitMQ channel could not be initialized before subscribing to {Queue}.", queueName);
            return Task.CompletedTask;
        }

        localChannel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
        localChannel.QueueBind(queue: queueName, exchange: options.Exchange, routingKey: routingKey);

        var consumer = new AsyncEventingBasicConsumer(localChannel);
        consumer.Received += async (_, args) =>
        {
            try
            {
                await handler(args.Body, cancellationToken).ConfigureAwait(false);
                localChannel.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message from {Queue}.", queueName);
                localChannel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
        };

        localChannel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        logger.LogInformation("RabbitMQ subscription started for queue {Queue} with routing key {RoutingKey}.", queueName, routingKey);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        channel?.Close();
        channel?.Dispose();
        connection?.Close();
        connection?.Dispose();
        return ValueTask.CompletedTask;
    }

    private bool EnsureChannel()
    {
        if (!options.Enabled)
        {
            return false;
        }

        if (channel is { IsClosed: false })
        {
            return true;
        }

        try
        {
            connection?.Dispose();
            channel?.Dispose();

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: options.Exchange, type: ExchangeType.Topic, durable: true, autoDelete: false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to establish RabbitMQ connection to {Host}:{Port}.", options.HostName, options.Port);
            connection = null;
            channel = null;
            return false;
        }
    }

    private static ConnectionFactory BuildFactory(RabbitMqOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new ConnectionFactory
            {
                Uri = new Uri(options.ConnectionString),
                DispatchConsumersAsync = true
            };
        }

        return new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            VirtualHost = options.VirtualHost,
            DispatchConsumersAsync = true,
            UserName = options.Username,
            Password = options.Password
        };
    }
}
