using GestorInventario.Application.Common.Interfaces;

namespace GestorInventario.Application.Tests.Helpers;

public sealed class StubCurrentUserService : ICurrentUserService
{
    public StubCurrentUserService(int? userId = null, string? userName = null)
    {
        UserId = userId;
        UserName = userName;
    }

    public int? UserId { get; set; }

    public string? UserName { get; set; }
}
