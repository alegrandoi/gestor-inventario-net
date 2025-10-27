using System;
using System.Text.Json;
using GestorInventario.Application.Common.Interfaces.Compliance;
using GestorInventario.Application.Inventory.IntegrationEvents;
using GestorInventario.Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Workers;

public class InventoryReplenishmentWorker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IMessageBus messageBus;
    private readonly DataAssetPolicy? inventoryPolicy;
    private readonly ILogger<InventoryReplenishmentWorker> logger;

    public InventoryReplenishmentWorker(
        IMessageBus messageBus,
        IDataGovernancePolicyRegistry policyRegistry,
        ILogger<InventoryReplenishmentWorker> logger)
    {
        this.messageBus = messageBus;
        this.logger = logger;
        policyRegistry.TryGetPolicyByAsset(DataGovernanceAssetKeys.InventoryTransactions, out inventoryPolicy);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await messageBus.SubscribeAsync(
            queueName: "inventory.replenishment",
            routingKey: "inventory.adjusted",
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
        var integrationEvent = JsonSerializer.Deserialize<InventoryAdjustedIntegrationEvent>(payload.Span, SerializerOptions);
        if (integrationEvent is null)
        {
            logger.LogWarning("Received invalid inventory adjustment payload");
            return Task.CompletedTask;
        }

        var classificationLabel = inventoryPolicy?.Classification.ToString() ?? DataClassification.None.ToString();
        var retentionDays = inventoryPolicy?.RetentionPeriod?.TotalDays;

        logger.LogInformation(
            "[Replenishment][{Classification}] SKU {Sku} adjusted by {Quantity} ({TransactionType}) | RetentionDays={RetentionDays}.",
            classificationLabel,
            integrationEvent.VariantSku,
            integrationEvent.Quantity,
            integrationEvent.TransactionType,
            retentionDays.HasValue ? Math.Round(retentionDays.Value) : 0);

        return Task.CompletedTask;
    }
}
