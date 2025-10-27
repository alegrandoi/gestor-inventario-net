using GestorInventario.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Controllers;

[Authorize]
public class RolesController : ApiControllerBase
{
    private readonly IIdentityService identityService;

    public RolesController(IIdentityService identityService)
    {
        this.identityService = identityService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<RoleDto>> GetRoles(CancellationToken cancellationToken)
    {
        return await identityService.GetRolesAsync(cancellationToken).ConfigureAwait(false);
    }
}
