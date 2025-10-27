using System.Text.Json;
using GestorInventario.Application.Common.Interfaces.Messaging;
using GestorInventario.Application.Common.Messaging;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Infrastructure.Messaging;

public class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IMessageBus messageBus;
    private readonly ILogger<IntegrationEventPublisher> logger;

    public IntegrationEventPublisher(IMessageBus messageBus, ILogger<IntegrationEventPublisher> logger)
    {
        this.messageBus = messageBus;
        this.logger = logger;
    }

    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, integrationEvent.GetType(), SerializerOptions);
        await messageBus.PublishAsync(integrationEvent.EventName, payload, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Integration event {EventName} dispatched.", integrationEvent.EventName);
    }
}
