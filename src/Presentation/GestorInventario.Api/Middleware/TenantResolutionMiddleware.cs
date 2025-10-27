using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GestorInventario.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate next;
    private const string TenantIdItemKey = "TenantId";
    private const string TenantCodeItemKey = "TenantCode";

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        IGestorInventarioDbContext dbContext,
        ILogger<TenantResolutionMiddleware> logger)
    {
        var cancellationToken = httpContext.RequestAborted;

        Tenant? resolvedTenant = await TryResolveTenantAsync(httpContext, dbContext, cancellationToken).ConfigureAwait(false);

        if (resolvedTenant is null)
        {
            resolvedTenant = await ResolveFallbackTenantAsync(dbContext, cancellationToken).ConfigureAwait(false);
        }

        if (resolvedTenant is null)
        {
            logger.LogWarning(
                "No se pudo resolver un inquilino activo para la solicitud {Path}. Se continuará sin filtrar por inquilino.",
                httpContext.Request.Path);

            httpContext.Items.Remove(TenantIdItemKey);
            httpContext.Items.Remove(TenantCodeItemKey);

            await next(httpContext).ConfigureAwait(false);
            return;
        }

        httpContext.Items[TenantIdItemKey] = resolvedTenant.Id;
        httpContext.Items[TenantCodeItemKey] = resolvedTenant.Code;

        await next(httpContext).ConfigureAwait(false);
    }

    private static async Task<Tenant?> TryResolveTenantAsync(
        HttpContext httpContext,
        IGestorInventarioDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var headers = httpContext.Request.Headers;

        if (headers.TryGetValue("X-Tenant-Id", out var tenantIdValues))
        {
            var requestedTenantId = tenantIdValues.FirstOrDefault();
            if (int.TryParse(requestedTenantId, out var tenantId) && tenantId > 0)
            {
                return await dbContext.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(tenant => tenant.Id == tenantId && tenant.IsActive, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (headers.TryGetValue("X-Tenant-Code", out var tenantCodeValues))
        {
            var tenantCode = tenantCodeValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tenantCode))
            {
                return await dbContext.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(tenant => tenant.Code == tenantCode && tenant.IsActive, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return null;
    }

    private static async Task<Tenant?> ResolveFallbackTenantAsync(
        IGestorInventarioDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.IsActive)
            .OrderBy(tenant => tenant.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
