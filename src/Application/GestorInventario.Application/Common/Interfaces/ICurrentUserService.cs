namespace GestorInventario.Application.Common.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }

    string? UserName { get; }
}
