using Predictorator.Models;
using Predictorator.Services;

namespace Predictorator.Endpoints;

public static class LoginEndpoints
{
    public static async Task<IResult> LoginAsync(LoginRequest? request, ISignInService signIn)
    {
        if (request is null)
        {
            return Results.BadRequest("Invalid request");
        }

        var result = await signIn.PasswordSignInAsync(request.Email, request.Password, request.RememberMe);
        if (result.Succeeded)
        {
            return Results.Ok();
        }
        if (result.IsLockedOut)
        {
            return Results.BadRequest("User locked out.");
        }
        return Results.BadRequest("Invalid login attempt.");
    }
}
