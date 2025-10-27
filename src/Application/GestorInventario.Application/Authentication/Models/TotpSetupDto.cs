namespace GestorInventario.Application.Authentication.Models;

public record TotpSetupDto(
    string Secret,
    string QrCodeUri);
