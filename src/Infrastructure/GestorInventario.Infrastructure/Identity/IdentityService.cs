using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GestorInventario.Application.Authentication.Commands;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Models;
using GestorInventario.Domain.Constants;
using GestorInventario.Domain.Entities;
using GestorInventario.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;

namespace GestorInventario.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly GestorInventarioDbContext context;
    private readonly UserManager<User> userManager;
    private readonly RoleManager<Role> roleManager;
    private readonly IJwtTokenGenerator jwtTokenGenerator;
    private readonly ILogger<IdentityService> logger;

    private const int TwoFactorSessionDurationMinutes = 5;
    private const int PasswordResetTokenDurationMinutes = 30;
    private const int RecoveryCodesCount = 10;
    private const string TotpIssuer = "GestorInventario";
    private const string RecoveryCodePrefix = "RECOVERY::";
    private static readonly VerificationWindow TotpVerificationWindow = new(previous: 1, future: 1);

    public IdentityService(
        GestorInventarioDbContext context,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<IdentityService> logger)
    {
        this.context = context;
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.jwtTokenGenerator = jwtTokenGenerator;
        this.logger = logger;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Username == request.Username || user.Email == request.Email, cancellationToken)
            .ConfigureAwait(false);

        if (existingUser is not null)
        {
            return Result<AuthResponseDto>.Failure("El usuario ya existe con ese nombre o correo electrónico.");
        }

        var role = await roleManager.FindByNameAsync(request.Role).ConfigureAwait(false);
        if (role is null)
        {
            return Result<AuthResponseDto>.Failure($"El rol '{request.Role}' no existe.");
        }

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            RoleId = role.Id,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            return Result<AuthResponseDto>.Failure(createResult.Errors.Select(error => error.Description));
        }

        await userManager.AddToRoleAsync(user, role.Name).ConfigureAwait(false);

        var token = await jwtTokenGenerator.CreateTokenAsync(user, new[] { role.Name }, cancellationToken)
            .ConfigureAwait(false);

        return Result<AuthResponseDto>.Success(token);
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(string usernameOrEmail, string password, CancellationToken cancellationToken)
    {
        var normalized = usernameOrEmail.Trim().ToUpperInvariant();

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(
                u => u.Username.ToUpper() == normalized || u.Email.ToUpper() == normalized,
                cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result<AuthResponseDto>.Failure("Credenciales inválidas.");
        }

        if (!user.IsActive)
        {
            return Result<AuthResponseDto>.Failure("La cuenta está desactivada. Contacta con un administrador.");
        }

        var isValidPassword = await userManager.CheckPasswordAsync(user, password).ConfigureAwait(false);
        if (!isValidPassword)
        {
            return Result<AuthResponseDto>.Failure("Credenciales inválidas.");
        }

        if (!user.IsMfaEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
        {
            ClearPendingTwoFactor(user);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            var token = await jwtTokenGenerator.CreateTokenAsync(user, roles.ToArray(), cancellationToken)
                .ConfigureAwait(false);
            return Result<AuthResponseDto>.Success(token);
        }

        var sessionId = GenerateSessionToken();
        user.PendingTwoFactorTokenHash = HashToken(user, sessionId);
        user.PendingTwoFactorTokenExpiresAt = DateTime.UtcNow.AddMinutes(TwoFactorSessionDurationMinutes);

        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var challenge = new AuthResponseDto(
            null,
            null,
            new UserSummaryDto(user.Id, user.Username, user.Email, user.Role?.Name ?? string.Empty, user.IsActive),
            true,
            sessionId,
            user.PendingTwoFactorTokenExpiresAt);

        return Result<AuthResponseDto>.Success(challenge);
    }

    public async Task<Result<AuthResponseDto>> CompleteTwoFactorLoginAsync(
        string usernameOrEmail,
        string sessionId,
        string verificationCode,
        CancellationToken cancellationToken)
    {
        var normalized = usernameOrEmail.Trim().ToUpperInvariant();

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(
                u => u.Username.ToUpper() == normalized || u.Email.ToUpper() == normalized,
                cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result<AuthResponseDto>.Failure("Sesión MFA inválida.");
        }

        if (!user.IsMfaEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
        {
            return Result<AuthResponseDto>.Failure("La autenticación multifactor no está habilitada para este usuario.");
        }

        if (user.PendingTwoFactorTokenExpiresAt is null || user.PendingTwoFactorTokenExpiresAt < DateTime.UtcNow)
        {
            ClearPendingTwoFactor(user);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<AuthResponseDto>.Failure("La sesión MFA ha expirado. Inicia sesión nuevamente.");
        }

        var sessionVerification = VerifyHashedToken(user, user.PendingTwoFactorTokenHash, sessionId);
        if (sessionVerification == PasswordVerificationResult.Failed)
        {
            return Result<AuthResponseDto>.Failure("Sesión MFA inválida.");
        }

        var sanitizedCode = (verificationCode ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal);

        var isTotpValid = ValidateTotp(user, sanitizedCode);
        var usedRecoveryCode = false;

        if (!isTotpValid)
        {
            isTotpValid = TryConsumeRecoveryCode(user, sanitizedCode, out usedRecoveryCode);
        }

        if (!isTotpValid)
        {
            return Result<AuthResponseDto>.Failure("Código MFA inválido.");
        }

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var token = await jwtTokenGenerator.CreateTokenAsync(user, roles.ToArray(), cancellationToken)
            .ConfigureAwait(false);

        ClearPendingTwoFactor(user);

        if (usedRecoveryCode)
        {
            logger.LogInformation("Se consumió un código de recuperación para el usuario {UserId} durante el acceso.", user.Id);
        }

        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<AuthResponseDto>.Success(token);
    }

    public async Task<Result<TotpSetupDto>> GenerateTotpSetupAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result<TotpSetupDto>.Failure("Usuario no encontrado.");
        }

        var secret = GenerateTotpSecret();
        user.TotpSecret = secret;
        user.IsMfaEnabled = false;
        user.TotpRecoveryCodes = null;
        ClearPendingTwoFactor(user);

        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var uri = BuildOtpAuthUri(user, secret);
        return Result<TotpSetupDto>.Success(new TotpSetupDto(secret, uri));
    }

    public async Task<Result<TotpActivationResultDto>> ActivateTotpAsync(
        int userId,
        string verificationCode,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result<TotpActivationResultDto>.Failure("Usuario no encontrado.");
        }

        if (string.IsNullOrWhiteSpace(user.TotpSecret))
        {
            return Result<TotpActivationResultDto>.Failure("No hay un secreto TOTP generado para este usuario.");
        }

        var sanitizedCode = (verificationCode ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal);
        if (!ValidateTotp(user, sanitizedCode))
        {
            return Result<TotpActivationResultDto>.Failure("El código TOTP proporcionado no es válido.");
        }

        var recoveryCodes = GenerateRecoveryCodes();
        StoreRecoveryCodes(user, recoveryCodes);
        user.IsMfaEnabled = true;

        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<TotpActivationResultDto>.Success(new TotpActivationResultDto(recoveryCodes));
    }

    public async Task<Result> DisableTotpAsync(int userId, string verificationCode, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Failure("Usuario no encontrado.");
        }

        if (!user.IsMfaEnabled)
        {
            return Result.Success();
        }

        var sanitizedCode = (verificationCode ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal);
        var isValid = ValidateTotp(user, sanitizedCode);
        if (!isValid)
        {
            isValid = TryConsumeRecoveryCode(user, sanitizedCode, out _);
        }

        if (!isValid)
        {
            return Result.Failure("Código MFA inválido.");
        }

        user.IsMfaEnabled = false;
        user.TotpSecret = null;
        user.TotpRecoveryCodes = null;
        ClearPendingTwoFactor(user);

        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    public async Task<Result<PasswordResetRequestDto>> InitiatePasswordResetAsync(
        string usernameOrEmail,
        CancellationToken cancellationToken)
    {
        var normalized = usernameOrEmail.Trim().ToUpperInvariant();

        var user = await context.Users
            .FirstOrDefaultAsync(
                u => u.Username.ToUpper() == normalized || u.Email.ToUpper() == normalized,
                cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result<PasswordResetRequestDto>.Failure("No se pudo generar un enlace de restablecimiento para las credenciales proporcionadas.");
        }

        var token = GenerateSessionToken();
        user.PasswordResetTokenHash = HashToken(user, token);
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(PasswordResetTokenDurationMinutes);

        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PasswordResetRequestDto>.Success(new PasswordResetRequestDto(
            token,
            user.PasswordResetTokenExpiresAt.Value,
            "email"));
    }

    public async Task<Result> ResetPasswordAsync(
        string usernameOrEmail,
        string token,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var normalized = usernameOrEmail.Trim().ToUpperInvariant();

        var user = await context.Users
            .FirstOrDefaultAsync(
                u => u.Username.ToUpper() == normalized || u.Email.ToUpper() == normalized,
                cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure("El token de restablecimiento no es válido o ha caducado.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordResetTokenHash) || user.PasswordResetTokenExpiresAt is null)
        {
            return Result.Failure("El token de restablecimiento no es válido o ha caducado.");
        }

        if (user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
        {
            return Result.Failure("El token de restablecimiento no es válido o ha caducado.");
        }

        var verification = VerifyHashedToken(user, user.PasswordResetTokenHash, token);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Result.Failure("El token de restablecimiento no es válido o ha caducado.");
        }

        foreach (var validator in userManager.PasswordValidators)
        {
            var validationResult = await validator.ValidateAsync(userManager, user, newPassword).ConfigureAwait(false);
            if (!validationResult.Succeeded)
            {
                return Result.Failure(validationResult.Errors.Select(error => error.Description));
            }
        }

        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, newPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
        ClearPendingTwoFactor(user);

        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    public async Task<UserSummaryDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return user is null
            ? null
            : new UserSummaryDto(user.Id, user.Username, user.Email, user.Role?.Name ?? string.Empty, user.IsActive);
    }

    public async Task<IReadOnlyCollection<RoleDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await context.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new RoleDto(role.Id, role.Name, role.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return roles;
    }

    public async Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await context.Users
            .Include(user => user.Role)
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return users
            .Select(user => new UserSummaryDto(user.Id, user.Username, user.Email, user.Role?.Name ?? string.Empty, user.IsActive))
            .ToList();
    }

    public async Task<Result<UserSummaryDto>> UpdateUserRoleAsync(int userId, string roleName, CancellationToken cancellationToken)
    {
        var user = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result<UserSummaryDto>.Failure("Usuario no encontrado.");
        }

        var role = await roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
        if (role is null)
        {
            return Result<UserSummaryDto>.Failure($"El rol '{roleName}' no existe.");
        }

        user.RoleId = role.Id;
        user.Role = role;

        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<UserSummaryDto>.Success(new UserSummaryDto(user.Id, user.Username, user.Email, role.Name, user.IsActive));
    }

    public async Task<Result<UserSummaryDto>> ToggleUserStatusAsync(int userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result<UserSummaryDto>.Failure("Usuario no encontrado.");
        }

        user.IsActive = isActive;
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<UserSummaryDto>.Success(new UserSummaryDto(
            user.Id,
            user.Username,
            user.Email,
            user.Role?.Name ?? string.Empty,
            user.IsActive));
    }

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in RoleNames.All)
        {
            if (await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
            {
                continue;
            }

            var role = new Role { Name = roleName, Description = $"Rol del sistema {roleName}" };
            var result = await roleManager.CreateAsync(role).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                logger.LogWarning("No se pudo crear el rol {Role}: {Errors}", roleName, string.Join(", ", result.Errors.Select(error => error.Description)));
            }
        }

        const string adminUsername = "admin";
        const string adminEmail = "admin@gestor-inventario.local";
        const string defaultPassword = "Admin123$";

        var adminRole = await roleManager.FindByNameAsync(RoleNames.Administrator).ConfigureAwait(false);
        if (adminRole is null)
        {
            logger.LogError("No se encontró el rol {Role} tras la inicialización de roles.", RoleNames.Administrator);
            return;
        }

        var adminUser = await userManager.FindByNameAsync(adminUsername).ConfigureAwait(false);
        if (adminUser is null)
        {
            adminUser = new User
            {
                Username = adminUsername,
                Email = adminEmail,
                IsActive = true,
                RoleId = adminRole.Id,
                Role = adminRole
            };

            var createResult = await userManager.CreateAsync(adminUser, defaultPassword).ConfigureAwait(false);
            if (!createResult.Succeeded)
            {
                logger.LogError("No se pudo crear el usuario administrador: {Errors}", string.Join(", ", createResult.Errors.Select(error => error.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, RoleNames.Administrator).ConfigureAwait(false))
        {
            var roleAssignResult = await userManager.AddToRoleAsync(adminUser, RoleNames.Administrator).ConfigureAwait(false);
            if (!roleAssignResult.Succeeded)
            {
                logger.LogError("No se pudo asignar el rol administrador al usuario admin: {Errors}", string.Join(", ", roleAssignResult.Errors.Select(error => error.Description)));
            }
        }
    }

    private static string GenerateSessionToken()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToHexString(buffer).ToLowerInvariant();
    }

    private static string GenerateTotpSecret()
    {
        var buffer = new byte[20];
        RandomNumberGenerator.Fill(buffer);
        return Base32Encoding.ToString(buffer).TrimEnd('=');
    }

    private static IReadOnlyCollection<string> GenerateRecoveryCodes()
    {
        var codes = new List<string>(RecoveryCodesCount);

        for (var index = 0; index < RecoveryCodesCount; index++)
        {
            var buffer = new byte[6];
            RandomNumberGenerator.Fill(buffer);
            codes.Add(Convert.ToHexString(buffer).ToLowerInvariant());
        }

        return codes;
    }

    private static string BuildOtpAuthUri(User user, string secret)
    {
        var account = string.IsNullOrWhiteSpace(user.Email) ? user.Username : user.Email;
        var label = Uri.EscapeDataString($"{TotpIssuer}:{account}");
        var issuer = Uri.EscapeDataString(TotpIssuer);
        return $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&digits=6";
    }

    private string HashToken(User user, string value) => userManager.PasswordHasher.HashPassword(user, value);

    private PasswordVerificationResult VerifyHashedToken(User user, string? hash, string value)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return PasswordVerificationResult.Failed;
        }

        return userManager.PasswordHasher.VerifyHashedPassword(user, hash, value);
    }

    private bool ValidateTotp(User user, string code)
    {
        if (string.IsNullOrWhiteSpace(user.TotpSecret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        if (!code.All(char.IsDigit))
        {
            return false;
        }

        try
        {
            var secretBytes = Base32Encoding.ToBytes(user.TotpSecret);
            var totp = new Totp(secretBytes, step: 30, totpSize: 6);
            return totp.VerifyTotp(code, out _, TotpVerificationWindow);
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "El secreto TOTP almacenado para el usuario {UserId} no es válido.", user.Id);
            return false;
        }
    }

    private void StoreRecoveryCodes(User user, IReadOnlyCollection<string> codes)
    {
        var hashedCodes = codes
            .Select(code => HashToken(user, RecoveryCodePrefix + code))
            .ToArray();

        user.TotpRecoveryCodes = hashedCodes.Length == 0 ? null : string.Join(';', hashedCodes);
    }

    private bool TryConsumeRecoveryCode(User user, string code, out bool consumed)
    {
        consumed = false;

        if (string.IsNullOrWhiteSpace(user.TotpRecoveryCodes) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var hashedCodes = user.TotpRecoveryCodes
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        for (var index = 0; index < hashedCodes.Count; index++)
        {
            var hashed = hashedCodes[index];
            var verification = VerifyHashedToken(user, hashed, RecoveryCodePrefix + code);

            if (verification is PasswordVerificationResult.Failed)
            {
                continue;
            }

            hashedCodes.RemoveAt(index);
            user.TotpRecoveryCodes = hashedCodes.Count == 0 ? null : string.Join(';', hashedCodes);
            consumed = true;
            return true;
        }

        return false;
    }

    private static void ClearPendingTwoFactor(User user)
    {
        user.PendingTwoFactorTokenHash = null;
        user.PendingTwoFactorTokenExpiresAt = null;
    }
}
