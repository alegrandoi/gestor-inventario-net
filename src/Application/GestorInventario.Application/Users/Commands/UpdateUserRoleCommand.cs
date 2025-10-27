using FluentValidation;
using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Models;
using GestorInventario.Domain.Constants;
using MediatR;

namespace GestorInventario.Application.Users.Commands;

public record UpdateUserRoleCommand(int UserId, string Role) : IRequest<UserSummaryDto>;

public class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
{
    public UpdateUserRoleCommandValidator()
    {
        RuleFor(command => command.UserId)
            .GreaterThan(0);

        RuleFor(command => command.Role)
            .NotEmpty()
            .Must(role => RoleNames.All.Contains(role))
            .WithMessage(command => $"El rol '{command.Role}' no es válido.");
    }
}

public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, UserSummaryDto>
{
    private readonly IIdentityService identityService;
    private readonly IPublisher publisher;

    public UpdateUserRoleCommandHandler(IIdentityService identityService, IPublisher publisher)
    {
        this.identityService = identityService;
        this.publisher = publisher;
    }

    public async Task<UserSummaryDto> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await identityService.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        var result = await identityService.UpdateUserRoleAsync(request.UserId, request.Role, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            var errorMessage = string.Join(Environment.NewLine, result.Errors);
            throw new Common.Exceptions.ValidationException(
                string.IsNullOrWhiteSpace(errorMessage) ? "No se pudo actualizar el rol del usuario." : errorMessage);
        }

        var updatedUser = result.Value;

        var previousRole = existingUser?.Role ?? string.Empty;
        if (!string.Equals(previousRole, updatedUser.Role, StringComparison.OrdinalIgnoreCase))
        {
            await publisher.Publish(
                new UserRoleChangedDomainEvent(
                    updatedUser.Id,
                    updatedUser.Username,
                    previousRole,
                    updatedUser.Role),
                cancellationToken).ConfigureAwait(false);
        }

        return updatedUser;
    }
}
