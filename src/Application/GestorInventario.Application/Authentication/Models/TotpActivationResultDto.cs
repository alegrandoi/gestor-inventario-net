namespace GestorInventario.Application.Authentication.Models;

public record TotpActivationResultDto(
    IReadOnlyCollection<string> RecoveryCodes);
