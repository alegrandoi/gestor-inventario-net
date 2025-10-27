using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using MediatR;

namespace GestorInventario.Application.Authentication.Commands;

public record ResetPasswordCommand(string UsernameOrEmail, string Token, string NewPassword) : IRequest;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.UsernameOrEmail)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Token)
            .NotEmpty();

        RuleFor(command => command.NewPassword)
            .NotEmpty();
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IIdentityService identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService)
    {
        this.identityService = identityService;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService
            .ResetPasswordAsync(request.UsernameOrEmail, request.Token, request.NewPassword, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            var errorMessage = string.Join(Environment.NewLine, result.Errors);
            throw new GestorInventario.Application.Common.Exceptions.ValidationException(
                string.IsNullOrWhiteSpace(errorMessage) ? "No se pudo restablecer la contraseña." : errorMessage);
        }

        return Unit.Value;
    }
}
