namespace Predictorator.Startup;

public static class StartupValidator
{
    public static StartupExitCode? Validate(WebApplicationBuilder builder)
    {
        if (builder.Environment.IsEnvironment("Testing"))
            return null;

        if (string.IsNullOrWhiteSpace(builder.Configuration["ApiSettings:RapidApiKey"]))
            return StartupExitCode.MissingRapidApiKey;

        return null;
    }
}
