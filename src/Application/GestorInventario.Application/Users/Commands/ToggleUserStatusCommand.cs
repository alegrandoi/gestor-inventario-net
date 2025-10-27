using FluentValidation;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Interfaces;
using MediatR;

namespace GestorInventario.Application.Users.Commands;

public record ToggleUserStatusCommand(int UserId, bool IsActive) : IRequest<UserSummaryDto>;

public class ToggleUserStatusCommandValidator : AbstractValidator<ToggleUserStatusCommand>
{
    public ToggleUserStatusCommandValidator()
    {
        RuleFor(command => command.UserId)
            .GreaterThan(0);
    }
}

public class ToggleUserStatusCommandHandler : IRequestHandler<ToggleUserStatusCommand, UserSummaryDto>
{
    private readonly IIdentityService identityService;

    public ToggleUserStatusCommandHandler(IIdentityService identityService)
    {
        this.identityService = identityService;
    }

    public async Task<UserSummaryDto> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.ToggleUserStatusAsync(request.UserId, request.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            var errorMessage = string.Join(Environment.NewLine, result.Errors);
            throw new Common.Exceptions.ValidationException(
                string.IsNullOrWhiteSpace(errorMessage) ? "No se pudo actualizar el estado del usuario." : errorMessage);
        }

        return result.Value;
    }
}
