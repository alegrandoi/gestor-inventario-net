using System.Security.Claims;
using GestorInventario.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GestorInventario.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var identifier = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(identifier, out var value))
            {
                return value;
            }

            return null;
        }
    }

    public string? UserName => httpContextAccessor.HttpContext?.User?.Identity?.Name;
}
