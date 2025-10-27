using GestorInventario.Domain.Entities;
using GestorInventario.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infrastructure.Identity;

public class UserStore :
    IUserPasswordStore<User>,
    IUserEmailStore<User>,
    IUserRoleStore<User>,
    IQueryableUserStore<User>
{
    private readonly GestorInventarioDbContext context;
    private readonly RoleManager<Role> roleManager;

    public UserStore(GestorInventarioDbContext context, RoleManager<Role> roleManager)
    {
        this.context = context;
        this.roleManager = roleManager;
    }

    public IQueryable<User> Users => context.Users.AsNoTracking();

    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return IdentityResult.Success;
    }

    public void Dispose()
    {
    }

    public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(userId, out var id))
        {
            return Task.FromResult<User?>(null);
        }

        return context.Users.Include(user => user.Role).FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return context.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Username.ToUpper() == normalizedUserName, cancellationToken);
    }

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Username.ToUpperInvariant());
    }

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id.ToString());
    }

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Username);
    }

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.Username = userName ?? string.Empty;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return IdentityResult.Success;
    }

    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(user.PasswordHash));
    }

    public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return context.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Email.ToUpper() == normalizedEmail, cancellationToken);
    }

    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Email.ToUpperInvariant());
    }

    public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        return SetRoleAsync(user, roleName, cancellationToken);
    }

    public Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        // In este dominio cada usuario posee un único rol obligatorio.
        // Cambiar a otro rol debe realizarse mediante UpdateUserRoleAsync.
        return Task.CompletedTask;
    }

    public Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
    {
        var roleName = user.Role?.Name;
        if (!string.IsNullOrWhiteSpace(roleName))
        {
            return Task.FromResult<IList<string>>(new List<string> { roleName });
        }

        if (user.RoleId != 0)
        {
            return context.Roles
                .Where(role => role.Id == user.RoleId)
                .Select(role => role.Name)
                .ToListAsync(cancellationToken)
                .ContinueWith(task => (IList<string>)task.Result, cancellationToken);
        }

        return Task.FromResult<IList<string>>(new List<string>());
    }

    public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        if (user.Role?.Name is not null)
        {
            return string.Equals(user.Role.Name, roleName, StringComparison.OrdinalIgnoreCase);
        }

        if (user.RoleId == 0)
        {
            return false;
        }

        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == user.RoleId, cancellationToken)
            .ConfigureAwait(false);
        return role is not null && string.Equals(role.Name, roleName, StringComparison.OrdinalIgnoreCase);
    }

    public Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        return context.Users
            .Include(user => user.Role)
            .Where(user => user.Role != null && user.Role.Name == roleName)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IList<User>)task.Result, cancellationToken);
    }

    private async Task SetRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
        if (role is null)
        {
            throw new InvalidOperationException($"El rol '{roleName}' no existe.");
        }

        user.RoleId = role.Id;
        user.Role = role;

        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
