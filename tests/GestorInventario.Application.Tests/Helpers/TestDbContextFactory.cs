using System.Threading;
using System.Threading.Tasks;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Interfaces.Compliance;
using GestorInventario.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Tests.Helpers;

public static class TestDbContextFactory
{
    public static GestorInventarioDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<GestorInventarioDbContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new GestorInventarioDbContext(options, new TestTenantService(), new NoOpDataGovernancePolicyEnforcer());
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class TestTenantService : ICurrentTenantService
    {
        public int? TenantId { get; set; } = 1;

        public string? TenantCode => null;
    }

    private sealed class NoOpDataGovernancePolicyEnforcer : IDataGovernancePolicyEnforcer
    {
        public Task EnforceAsync(IGestorInventarioDbContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
