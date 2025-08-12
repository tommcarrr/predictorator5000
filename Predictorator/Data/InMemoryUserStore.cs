using Microsoft.AspNetCore.Identity;
using System.Collections.Concurrent;

namespace Predictorator.Data;

public class InMemoryUserStore :
    IUserPasswordStore<IdentityUser>,
    IUserRoleStore<IdentityUser>,
    IUserEmailStore<IdentityUser>,
    IUserSecurityStampStore<IdentityUser>
{
    private readonly ConcurrentDictionary<string, IdentityUser> _users = new();
    private readonly ConcurrentDictionary<string, string> _passwords = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _roles = new();

    public Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        _users[user.Id] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        _users.TryRemove(user.Id, out _);
        _passwords.TryRemove(user.Id, out _);
        _roles.TryRemove(user.Id, out _);
        return Task.FromResult(IdentityResult.Success);
    }

    public void Dispose() { }

    public Task<IdentityUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        => Task.FromResult(_users.TryGetValue(userId, out var user) ? user : null);

    public Task<IdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        => Task.FromResult(_users.Values.FirstOrDefault(u => u.NormalizedUserName == normalizedUserName));

    public Task<string?> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedUserName);

    public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName);

    public Task SetNormalizedUserNameAsync(IdentityUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(IdentityUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        _users[user.Id] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task SetPasswordHashAsync(IdentityUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        if (passwordHash is not null)
            _passwords[user.Id] = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        _passwords.TryGetValue(user.Id, out var hash);
        return Task.FromResult(hash);
    }

    public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(_passwords.ContainsKey(user.Id));

    public Task SetEmailAsync(IdentityUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(IdentityUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task<IdentityUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        => Task.FromResult(_users.Values.FirstOrDefault(u => u.NormalizedEmail == normalizedEmail));

    public Task<string?> GetNormalizedEmailAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(IdentityUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public Task AddToRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        var roles = _roles.GetOrAdd(user.Id, _ => new HashSet<string>());
        roles.Add(roleName);
        return Task.CompletedTask;
    }

    public Task RemoveFromRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        if (_roles.TryGetValue(user.Id, out var roles))
            roles.Remove(roleName);
        return Task.CompletedTask;
    }

    public Task<IList<string>> GetRolesAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        if (_roles.TryGetValue(user.Id, out var roles))
            return Task.FromResult<IList<string>>(roles.ToList());
        return Task.FromResult<IList<string>>(new List<string>());
    }

    public Task<bool> IsInRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
        => Task.FromResult(_roles.TryGetValue(user.Id, out var roles) && roles.Contains(roleName));

    public Task<IList<IdentityUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var users = _roles.Where(kvp => kvp.Value.Contains(roleName))
            .Select(kvp => _users[kvp.Key])
            .ToList();
        return Task.FromResult<IList<IdentityUser>>(users);
    }

    public Task SetSecurityStampAsync(IdentityUser user, string? stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.SecurityStamp);
}
