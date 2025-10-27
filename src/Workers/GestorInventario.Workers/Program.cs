using GestorInventario.Application;
using GestorInventario.Infrastructure;
using GestorInventario.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddApplication();
        services.AddInfrastructure(context.Configuration);
        services.AddHostedService<InventoryReplenishmentWorker>();
        services.AddHostedService<LogisticsSyncWorker>();
        services.AddHostedService<AnalyticsSimulationWorker>();
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);
