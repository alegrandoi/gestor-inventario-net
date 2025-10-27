using FluentValidation;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using MediatR;

namespace GestorInventario.Application.Authentication.Commands;

public record ActivateTotpCommand(int UserId, string VerificationCode) : IRequest<TotpActivationResultDto>;

public class ActivateTotpCommandValidator : AbstractValidator<ActivateTotpCommand>
{
    public ActivateTotpCommandValidator()
    {
        RuleFor(command => command.UserId)
            .GreaterThan(0);

        RuleFor(command => command.VerificationCode)
            .NotEmpty();
    }
}

public class ActivateTotpCommandHandler : IRequestHandler<ActivateTotpCommand, TotpActivationResultDto>
{
    private readonly IIdentityService identityService;

    public ActivateTotpCommandHandler(IIdentityService identityService)
    {
        this.identityService = identityService;
    }

    public async Task<TotpActivationResultDto> Handle(ActivateTotpCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService
            .ActivateTotpAsync(request.UserId, request.VerificationCode, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            var errorMessage = string.Join(Environment.NewLine, result.Errors);
            throw new GestorInventario.Application.Common.Exceptions.ValidationException(
                string.IsNullOrWhiteSpace(errorMessage) ? "No se pudo activar el MFA." : errorMessage);
        }

        return result.Value;
    }
}
