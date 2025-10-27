namespace GestorInventario.Application.Authentication.Models;

public record UserSummaryDto(
    int Id,
    string Username,
    string Email,
    string Role,
    bool IsActive
);
