using System.Security.Claims;
using GestorInventario.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GestorInventario.Infrastructure.Identity;

public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public int? TenantId
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            if (httpContext.Items.TryGetValue("TenantId", out var tenantObj) && tenantObj is int tenantIdFromItem)
            {
                return tenantIdFromItem;
            }

            if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader)
                && int.TryParse(tenantHeader.FirstOrDefault(), out var tenantIdFromHeader))
            {
                return tenantIdFromHeader;
            }

            var tenantClaim = httpContext.User?.FindFirstValue("tenant_id");
            if (int.TryParse(tenantClaim, out var tenantIdFromClaim))
            {
                return tenantIdFromClaim;
            }

            return null;
        }
    }

    public string? TenantCode
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            if (httpContext.Items.TryGetValue("TenantCode", out var tenantCodeObj) && tenantCodeObj is string tenantCode)
            {
                return tenantCode;
            }

            if (httpContext.Request.Headers.TryGetValue("X-Tenant-Code", out var tenantCodeHeader))
            {
                return tenantCodeHeader.FirstOrDefault();
            }

            return httpContext.User?.FindFirstValue("tenant_code");
        }
    }
}
