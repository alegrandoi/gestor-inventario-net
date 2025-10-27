using FluentValidation;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using MediatR;

namespace GestorInventario.Application.Authentication.Commands;

public record CompleteMfaLoginCommand(string UsernameOrEmail, string SessionId, string VerificationCode) : IRequest<AuthResponseDto>;

public class CompleteMfaLoginCommandValidator : AbstractValidator<CompleteMfaLoginCommand>
{
    public CompleteMfaLoginCommandValidator()
    {
        RuleFor(command => command.UsernameOrEmail)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.SessionId)
            .NotEmpty();

        RuleFor(command => command.VerificationCode)
            .NotEmpty();
    }
}

public class CompleteMfaLoginCommandHandler : IRequestHandler<CompleteMfaLoginCommand, AuthResponseDto>
{
    private readonly IIdentityService identityService;

    public CompleteMfaLoginCommandHandler(IIdentityService identityService)
    {
        this.identityService = identityService;
    }

    public async Task<AuthResponseDto> Handle(CompleteMfaLoginCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService
            .CompleteTwoFactorLoginAsync(request.UsernameOrEmail, request.SessionId, request.VerificationCode, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            var errorMessage = string.Join(Environment.NewLine, result.Errors);
            throw new GestorInventario.Application.Common.Exceptions.ValidationException(
                string.IsNullOrWhiteSpace(errorMessage) ? "No se pudo validar el código MFA." : errorMessage);
        }

        return result.Value;
    }
}
