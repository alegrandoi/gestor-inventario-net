using FluentValidation;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Models;
using GestorInventario.Domain.Constants;
using MediatR;

namespace GestorInventario.Application.Authentication.Commands;

public record RegisterUserCommand(
    string Username,
    string Email,
    string Password,
    string Role
) : IRequest<AuthResponseDto>;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100);

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(6);

        RuleFor(command => command.Role)
            .NotEmpty()
            .Must(role => RoleNames.All.Contains(role))
            .WithMessage(command => $"El rol '{command.Role}' no es válido.");
    }
}

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponseDto>
{
    private readonly IIdentityService identityService;

    public RegisterUserCommandHandler(IIdentityService identityService)
    {
        this.identityService = identityService;
    }

    public async Task<AuthResponseDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.RegisterAsync(request, cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            var errorMessage = string.Join(Environment.NewLine, result.Errors);
            throw new Common.Exceptions.ValidationException(
                string.IsNullOrWhiteSpace(errorMessage) ? "No se pudo registrar el usuario." : errorMessage);
        }

        return result.Value;
    }
}
