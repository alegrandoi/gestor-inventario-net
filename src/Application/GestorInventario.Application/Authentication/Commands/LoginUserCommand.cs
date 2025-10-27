using FluentValidation;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Interfaces;
using MediatR;

namespace GestorInventario.Application.Authentication.Commands;

public record LoginUserCommand(string UsernameOrEmail, string Password) : IRequest<AuthResponseDto>;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(command => command.UsernameOrEmail)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Password)
            .NotEmpty();
    }
}

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResponseDto>
{
    private readonly IIdentityService identityService;

    public LoginUserCommandHandler(IIdentityService identityService)
    {
        this.identityService = identityService;
    }

    public async Task<AuthResponseDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.LoginAsync(request.UsernameOrEmail, request.Password, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            var errorMessage = string.Join(Environment.NewLine, result.Errors);
            throw new Common.Exceptions.ValidationException(
                string.IsNullOrWhiteSpace(errorMessage) ? "Credenciales inválidas." : errorMessage);
        }

        return result.Value;
    }
}
