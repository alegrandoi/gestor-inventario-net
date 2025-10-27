using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using MediatR;

namespace GestorInventario.Application.Authentication.Commands;

public record DisableTotpCommand(int UserId, string VerificationCode) : IRequest;

public class DisableTotpCommandValidator : AbstractValidator<DisableTotpCommand>
{
    public DisableTotpCommandValidator()
    {
        RuleFor(command => command.UserId)
            .GreaterThan(0);

        RuleFor(command => command.VerificationCode)
            .NotEmpty();
    }
}

public class DisableTotpCommandHandler : IRequestHandler<DisableTotpCommand>
{
    private readonly IIdentityService identityService;

    public DisableTotpCommandHandler(IIdentityService identityService)
    {
        this.identityService = identityService;
    }

    public async Task<Unit> Handle(DisableTotpCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService
            .DisableTotpAsync(request.UserId, request.VerificationCode, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            var errorMessage = string.Join(Environment.NewLine, result.Errors);
            throw new GestorInventario.Application.Common.Exceptions.ValidationException(
                string.IsNullOrWhiteSpace(errorMessage) ? "No se pudo desactivar el MFA." : errorMessage);
        }

        return Unit.Value;
    }
}
