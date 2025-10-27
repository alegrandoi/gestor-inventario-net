using GestorInventario.Domain.Entities;
using GestorInventario.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infrastructure.Identity;

public class RoleStore : IRoleStore<Role>, IQueryableRoleStore<Role>
{
    private readonly GestorInventarioDbContext context;

    public RoleStore(GestorInventarioDbContext context)
    {
        this.context = context;
    }

    public IQueryable<Role> Roles => context.Roles.AsQueryable();

    public async Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
    {
        context.Roles.Add(role);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
    {
        context.Roles.Remove(role);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return IdentityResult.Success;
    }

    public void Dispose()
    {
    }

    public Task<Role?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(roleId, out var id))
        {
            return Task.FromResult<Role?>(null);
        }

        return context.Roles.FirstOrDefaultAsync(role => role.Id == id, cancellationToken);
    }

    public Task<Role?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        return context.Roles.FirstOrDefaultAsync(role => role.Name.ToUpper() == normalizedRoleName, cancellationToken);
    }

    public Task<string?> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(role.Name?.ToUpperInvariant());
    }

    public Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Id.ToString());
    }

    public Task<string?> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(role.Name);
    }

    public Task SetNormalizedRoleNameAsync(Role role, string? normalizedName, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task SetRoleNameAsync(Role role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName ?? string.Empty;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
    {
        context.Roles.Update(role);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return IdentityResult.Success;
    }
}
