using System.Text.Json;
using GestorInventario.Application.Common.Interfaces.Compliance;
using GestorInventario.Application.SalesOrders.IntegrationEvents;
using GestorInventario.Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Workers;

public class LogisticsSyncWorker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IMessageBus messageBus;
    private readonly ILogger<LogisticsSyncWorker> logger;
    private readonly DataAssetPolicy? salesOrderPolicy;

    public LogisticsSyncWorker(
        IMessageBus messageBus,
        IDataGovernancePolicyRegistry policyRegistry,
        ILogger<LogisticsSyncWorker> logger)
    {
        this.messageBus = messageBus;
        this.logger = logger;
        policyRegistry.TryGetPolicyByAsset(DataGovernanceAssetKeys.SalesOrders, out salesOrderPolicy);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await messageBus.SubscribeAsync(
            queueName: "logistics.sync",
            routingKey: "salesorder.changed",
            handler: HandleMessageAsync,
            cancellationToken: stoppingToken).ConfigureAwait(false);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }

    private Task HandleMessageAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var integrationEvent = JsonSerializer.Deserialize<SalesOrderChangedIntegrationEvent>(payload.Span, SerializerOptions);
        if (integrationEvent is null)
        {
            logger.LogWarning("Received invalid sales order payload");
            return Task.CompletedTask;
        }

        var classificationLabel = salesOrderPolicy?.Classification.ToString() ?? DataClassification.None.ToString();
        var safeCustomer = salesOrderPolicy?.ContainsPersonalData == true
            ? "REDACTED"
            : integrationEvent.CustomerName;

        logger.LogInformation(
            "[Logistics][{Classification}] Order {OrderId} now {Status} for {Customer}.",
            classificationLabel,
            integrationEvent.OrderId,
            integrationEvent.Status,
            safeCustomer);

        return Task.CompletedTask;
    }
}
