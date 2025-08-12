using Microsoft.AspNetCore.Builder;
using Predictorator.Startup;

namespace Predictorator.Tests;

public class StartupValidatorTests
{
    [Fact]
    public void Returns_error_when_RapidApiKey_missing()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Production"
        });
        builder.Configuration["Resend:ApiToken"] = "token";
        builder.Configuration["ApiSettings:RapidApiKey"] = null;

        var result = StartupValidator.Validate(builder);

        Assert.Equal(StartupExitCode.MissingRapidApiKey, result);
    }

    [Fact]
    public void Returns_null_when_testing_environment()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });

        var result = StartupValidator.Validate(builder);

        Assert.Null(result);
    }
}
