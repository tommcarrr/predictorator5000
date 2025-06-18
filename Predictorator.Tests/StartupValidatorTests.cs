using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Predictorator.Startup;
using System.Collections.Generic;

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
        builder.Configuration["ConnectionStrings:DefaultConnection"] = "Data Source=test.db";
        builder.Configuration["Resend:ApiToken"] = "token";

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
