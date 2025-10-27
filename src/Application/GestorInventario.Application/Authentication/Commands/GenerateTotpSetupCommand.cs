using FluentValidation;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using MediatR;

namespace GestorInventario.Application.Authentication.Commands;

public record GenerateTotpSetupCommand(int UserId) : IRequest<TotpSetupDto>;

public class GenerateTotpSetupCommandValidator : AbstractValidator<GenerateTotpSetupCommand>
{
    public GenerateTotpSetupCommandValidator()
    {
        RuleFor(command => command.UserId)
            .GreaterThan(0);
    }
}

public class GenerateTotpSetupCommandHandler : IRequestHandler<GenerateTotpSetupCommand, TotpSetupDto>
{
    private readonly IIdentityService identityService;

    public GenerateTotpSetupCommandHandler(IIdentityService identityService)
    {
        this.identityService = identityService;
    }

    public async Task<TotpSetupDto> Handle(GenerateTotpSetupCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.GenerateTotpSetupAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            var errorMessage = string.Join(Environment.NewLine, result.Errors);
            throw new GestorInventario.Application.Common.Exceptions.ValidationException(
                string.IsNullOrWhiteSpace(errorMessage) ? "No se pudo generar el secreto MFA." : errorMessage);
        }

        return result.Value;
    }
}
