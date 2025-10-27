using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Interfaces.Compliance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Infrastructure.DataGovernance;

public sealed class DataGovernancePolicyEnforcer : IDataGovernancePolicyEnforcer
{
    private readonly IDataGovernancePolicyRegistry policyRegistry;
    private readonly ILogger<DataGovernancePolicyEnforcer> logger;

    public DataGovernancePolicyEnforcer(
        IDataGovernancePolicyRegistry policyRegistry,
        ILogger<DataGovernancePolicyEnforcer> logger)
    {
        this.policyRegistry = policyRegistry;
        this.logger = logger;
    }

    public async Task EnforceAsync(IGestorInventarioDbContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await EnforceAuditLogRetentionAsync(context, now, cancellationToken).ConfigureAwait(false);
        await EnforceInventoryTransactionRetentionAsync(context, now, cancellationToken).ConfigureAwait(false);
        await EnforceSalesOrderSanitisationAsync(context, now, cancellationToken).ConfigureAwait(false);
        await EnforceCustomerAnonymisationAsync(context, now, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnforceAuditLogRetentionAsync(IGestorInventarioDbContext context, DateTime now, CancellationToken cancellationToken)
    {
        if (!policyRegistry.TryGetPolicyByAsset(DataGovernanceAssetKeys.AuditLogs, out var policy) || policy.RetentionPeriod is null)
        {
            return;
        }

        var cutoff = now - policy.RetentionPeriod.Value;
        var expiredLogs = await context.AuditLogs
            .Where(log => log.CreatedAt < cutoff)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (expiredLogs.Count == 0)
        {
            return;
        }

        context.AuditLogs.RemoveRange(expiredLogs);
        logger.LogInformation(
            "Applied retention policy {AssetKey}: removed {Count} audit entries older than {Cutoff} (reference: {Reference}).",
            policy.AssetKey,
            expiredLogs.Count,
            cutoff.ToString("O", CultureInfo.InvariantCulture),
            policy.RetentionReference);
    }

    private async Task EnforceInventoryTransactionRetentionAsync(IGestorInventarioDbContext context, DateTime now, CancellationToken cancellationToken)
    {
        if (!policyRegistry.TryGetPolicyByAsset(DataGovernanceAssetKeys.InventoryTransactions, out var policy) || policy.RetentionPeriod is null)
        {
            return;
        }

        var cutoff = now - policy.RetentionPeriod.Value;
        var expiredTransactions = await context.InventoryTransactions
            .Where(transaction => transaction.TransactionDate < cutoff)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (expiredTransactions.Count == 0)
        {
            return;
        }

        context.InventoryTransactions.RemoveRange(expiredTransactions);
        logger.LogInformation(
            "Applied retention policy {AssetKey}: removed {Count} inventory transactions older than {Cutoff} (reference: {Reference}).",
            policy.AssetKey,
            expiredTransactions.Count,
            cutoff.ToString("O", CultureInfo.InvariantCulture),
            policy.RetentionReference);
    }

    private async Task EnforceSalesOrderSanitisationAsync(IGestorInventarioDbContext context, DateTime now, CancellationToken cancellationToken)
    {
        if (!policyRegistry.TryGetPolicyByAsset(DataGovernanceAssetKeys.SalesOrders, out var policy) || policy.RetentionPeriod is null)
        {
            return;
        }

        var cutoff = now - policy.RetentionPeriod.Value;
        var ordersToSanitise = await context.SalesOrders
            .Where(order => order.OrderDate < cutoff && (order.ShippingAddress != null || order.Notes != null))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (ordersToSanitise.Count == 0)
        {
            return;
        }

        foreach (var order in ordersToSanitise)
        {
            order.ShippingAddress = null;
            order.Notes = null;
        }

        logger.LogInformation(
            "Applied sanitisation for {AssetKey}: scrubbed addresses for {Count} historical orders prior to {Cutoff}.",
            policy.AssetKey,
            ordersToSanitise.Count,
            cutoff.ToString("O", CultureInfo.InvariantCulture));
    }

    private async Task EnforceCustomerAnonymisationAsync(IGestorInventarioDbContext context, DateTime now, CancellationToken cancellationToken)
    {
        if (!policyRegistry.TryGetPolicyByAsset(DataGovernanceAssetKeys.Customers, out var policy) || policy.RetentionPeriod is null)
        {
            return;
        }

        var cutoff = now - policy.RetentionPeriod.Value;
        var customersToAnonymise = await context.Customers
            .Where(customer => (customer.UpdatedAt ?? customer.CreatedAt) < cutoff)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (customersToAnonymise.Count == 0)
        {
            return;
        }

        foreach (var customer in customersToAnonymise)
        {
            customer.Name = $"Anon-{customer.Id:D6}";
            customer.Email = null;
            customer.Phone = null;
            customer.Address = null;
            customer.Notes = null;
        }

        logger.LogInformation(
            "Applied anonymisation for {AssetKey}: scrubbed personal fields for {Count} inactive customers prior to {Cutoff}.",
            policy.AssetKey,
            customersToAnonymise.Count,
            cutoff.ToString("O", CultureInfo.InvariantCulture));
    }
}
