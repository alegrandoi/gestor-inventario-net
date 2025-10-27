using System;
using System.Linq;
using System.Text.Json;
using GestorInventario.Application.Analytics.IntegrationEvents;
using GestorInventario.Application.Analytics.Queries;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Interfaces.Compliance;
using GestorInventario.Infrastructure.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Workers;

public class AnalyticsSimulationWorker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<AnalyticsSimulationWorker> logger;
    private readonly DataAssetPolicy? salesOrderPolicy;

    public AnalyticsSimulationWorker(
        IServiceScopeFactory scopeFactory,
        IDataGovernancePolicyRegistry policyRegistry,
        ILogger<AnalyticsSimulationWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        policyRegistry.TryGetPolicyByAsset(DataGovernanceAssetKeys.SalesOrders, out salesOrderPolicy);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing analytics simulation batch.");
            }

            try
            {
                await Task.Delay(RefreshInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IGestorInventarioDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var candidateVariants = await context.ProductVariants
            .Include(variant => variant.Product)
            .Where(variant => variant.Product != null && variant.Product.IsActive)
            .OrderByDescending(variant => variant.Product!.LeadTimeDays ?? 0)
            .ThenBy(variant => variant.Id)
            .Take(5)
            .Select(variant => variant.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (candidateVariants.Count == 0)
        {
            logger.LogDebug("No variants available for analytics simulation batch.");
            return;
        }

        foreach (var variantId in candidateVariants)
        {
            var comparison = await mediator.Send(
                new CompareOptimizationScenariosQuery(
                    variantId,
                    MonteCarloIterations: 400,
                    Scenarios: Array.Empty<OptimizationScenarioInput>()),
                cancellationToken).ConfigureAwait(false);

            var integrationEvent = new AnalyticsSimulationCompletedIntegrationEvent(
                comparison.GeneratedAt,
                comparison.Variant,
                comparison.Baseline.Policy,
                comparison.Baseline.Kpis,
                comparison.Baseline.MonteCarlo,
                "worker.analytics");

            var payload = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, integrationEvent.GetType(), SerializerOptions);
            await messageBus.PublishAsync(integrationEvent.EventName, payload, cancellationToken).ConfigureAwait(false);

            var classificationLabel = salesOrderPolicy?.Classification.ToString() ?? DataClassification.None.ToString();
            logger.LogInformation(
                "[Analytics][{Classification}] Published simulation snapshot for variant {VariantId} ({Sku}).",
                classificationLabel,
                comparison.Variant.VariantId,
                comparison.Variant.VariantSku);
        }
    }
}
