namespace GestorInventario.Application.Authentication.Models;

public record PasswordResetRequestDto(
    string Token,
    DateTime ExpiresAt,
    string DeliveryChannel);
