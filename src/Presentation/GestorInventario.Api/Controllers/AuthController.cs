using GestorInventario.Application.Authentication.Commands;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Authentication.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestorInventario.Api.Controllers;

public class AuthController : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var response = await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return Created(string.Empty, response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<AuthResponseDto> Login([FromBody] LoginUserCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("login/mfa")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<AuthResponseDto> CompleteMfaLogin([FromBody] CompleteMfaLoginCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("password/forgot")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasswordResetRequestDto), StatusCodes.Status200OK)]
    public async Task<PasswordResetRequestDto> InitiatePasswordReset([FromBody] InitiatePasswordResetCommand command, CancellationToken cancellationToken)
    {
        return await Sender.Send(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("password/reset")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await Sender.Send(command, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("mfa/setup")]
    [Authorize]
    [ProducesResponseType(typeof(TotpSetupDto), StatusCodes.Status200OK)]
    public async Task<TotpSetupDto> GenerateTotpSetup(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return await Sender.Send(new GenerateTotpSetupCommand(userId), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("mfa/activate")]
    [Authorize]
    [ProducesResponseType(typeof(TotpActivationResultDto), StatusCodes.Status200OK)]
    public async Task<TotpActivationResultDto> ActivateTotp([FromBody] TotpVerificationRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return await Sender.Send(new ActivateTotpCommand(userId, request.VerificationCode), cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DisableTotp([FromBody] TotpVerificationRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await Sender.Send(new DisableTotpCommand(userId, request.VerificationCode), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var user = await Sender.Send(new GetCurrentUserQuery(userId), cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    private int GetCurrentUserId()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            throw new UnauthorizedAccessException("No se pudo resolver el identificador de usuario.");
        }

        return userId;
    }

    public record TotpVerificationRequest(string VerificationCode);
}
