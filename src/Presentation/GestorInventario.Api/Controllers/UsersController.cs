using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Users.Commands;
using GestorInventario.Application.Users.Queries;
using GestorInventario.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class UsersController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<UserSummaryDto>> GetUsers(CancellationToken cancellationToken)
    {
        return await Sender.Send(new GetUsersQuery(), cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("{id:int}/role")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    public async Task<UserSummaryDto> UpdateRole(int id, [FromBody] UpdateUserRoleCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { UserId = id }, cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    public async Task<UserSummaryDto> UpdateStatus(int id, [FromBody] ToggleUserStatusCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command with { UserId = id }, cancellationToken).ConfigureAwait(false);
    }
}
