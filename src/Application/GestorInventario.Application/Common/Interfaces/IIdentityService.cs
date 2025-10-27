using GestorInventario.Application.Authentication.Commands;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Models;

namespace GestorInventario.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterUserCommand request, CancellationToken cancellationToken);

    Task<Result<AuthResponseDto>> LoginAsync(string usernameOrEmail, string password, CancellationToken cancellationToken);

    Task<Result<AuthResponseDto>> CompleteTwoFactorLoginAsync(
        string usernameOrEmail,
        string sessionId,
        string verificationCode,
        CancellationToken cancellationToken);

    Task<Result<TotpSetupDto>> GenerateTotpSetupAsync(int userId, CancellationToken cancellationToken);

    Task<Result<TotpActivationResultDto>> ActivateTotpAsync(
        int userId,
        string verificationCode,
        CancellationToken cancellationToken);

    Task<Result> DisableTotpAsync(int userId, string verificationCode, CancellationToken cancellationToken);

    Task<Result<PasswordResetRequestDto>> InitiatePasswordResetAsync(
        string usernameOrEmail,
        CancellationToken cancellationToken);

    Task<Result> ResetPasswordAsync(
        string usernameOrEmail,
        string token,
        string newPassword,
        CancellationToken cancellationToken);

    Task<UserSummaryDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RoleDto>> GetRolesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken);

    Task<Result<UserSummaryDto>> UpdateUserRoleAsync(int userId, string roleName, CancellationToken cancellationToken);

    Task<Result<UserSummaryDto>> ToggleUserStatusAsync(int userId, bool isActive, CancellationToken cancellationToken);

    Task EnsureSeedDataAsync(CancellationToken cancellationToken);
}

public record RoleDto(int Id, string Name, string? Description);
