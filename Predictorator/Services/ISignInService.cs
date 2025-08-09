using Microsoft.AspNetCore.Identity;

namespace Predictorator.Services;

public interface ISignInService
{
    Task<SignInResult> PasswordSignInAsync(string email, string password);
}
