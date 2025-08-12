using Microsoft.AspNetCore.Identity;
using System.Collections.Concurrent;

namespace Predictorator.Data;

public class InMemoryRoleStore : IRoleStore<IdentityRole>
{
    private readonly ConcurrentDictionary<string, IdentityRole> _roles = new();

    public Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        _roles[role.Id] = role;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        _roles.TryRemove(role.Id, out _);
        return Task.FromResult(IdentityResult.Success);
    }

    public void Dispose() { }

    public Task<IdentityRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        => Task.FromResult(_roles.TryGetValue(roleId, out var role) ? role : null);

    public Task<IdentityRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        => Task.FromResult(_roles.Values.FirstOrDefault(r => r.NormalizedName == normalizedRoleName));

    public Task<string?> GetNormalizedRoleNameAsync(IdentityRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.NormalizedName);

    public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.Id);

    public Task<string?> GetRoleNameAsync(IdentityRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.Name);

    public Task SetNormalizedRoleNameAsync(IdentityRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetRoleNameAsync(IdentityRole role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        _roles[role.Id] = role;
        return Task.FromResult(IdentityResult.Success);
    }
}
