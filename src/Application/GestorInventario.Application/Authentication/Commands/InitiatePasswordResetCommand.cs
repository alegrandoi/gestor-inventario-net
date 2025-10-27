using FluentValidation;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using MediatR;

namespace GestorInventario.Application.Authentication.Commands;

public record InitiatePasswordResetCommand(string UsernameOrEmail) : IRequest<PasswordResetRequestDto>;

public class InitiatePasswordResetCommandValidator : AbstractValidator<InitiatePasswordResetCommand>
{
    public InitiatePasswordResetCommandValidator()
    {
        RuleFor(command => command.UsernameOrEmail)
            .NotEmpty()
            .MaximumLength(200);
    }
}

public class InitiatePasswordResetCommandHandler : IRequestHandler<InitiatePasswordResetCommand, PasswordResetRequestDto>
{
    private readonly IIdentityService identityService;

    public InitiatePasswordResetCommandHandler(IIdentityService identityService)
    {
        this.identityService = identityService;
    }

    public async Task<PasswordResetRequestDto> Handle(InitiatePasswordResetCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService
            .InitiatePasswordResetAsync(request.UsernameOrEmail, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            var errorMessage = string.Join(Environment.NewLine, result.Errors);
            throw new GestorInventario.Application.Common.Exceptions.ValidationException(
                string.IsNullOrWhiteSpace(errorMessage) ? "No se pudo iniciar el restablecimiento de contraseña." : errorMessage);
        }

        return result.Value;
    }
}
