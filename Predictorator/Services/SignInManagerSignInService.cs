using Microsoft.AspNetCore.Identity;

namespace Predictorator.Services;

public class SignInManagerSignInService : ISignInService
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public SignInManagerSignInService(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public Task<SignInResult> PasswordSignInAsync(string email, string password, bool rememberMe)
    {
        return _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
    }
}
