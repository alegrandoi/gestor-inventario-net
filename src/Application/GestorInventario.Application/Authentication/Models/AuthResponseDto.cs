namespace GestorInventario.Application.Authentication.Models;

public record AuthResponseDto(
    string? Token,
    DateTime? ExpiresAt,
    UserSummaryDto? User,
    bool RequiresTwoFactor,
    string? TwoFactorSessionId,
    DateTime? SessionExpiresAt
);
