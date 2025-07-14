namespace Predictorator.Startup;

public static class StartupValidator
{
    public static StartupExitCode? Validate(WebApplicationBuilder builder)
    {
        if (builder.Environment.IsEnvironment("Testing"))
            return null;

        if (string.IsNullOrWhiteSpace(builder.Configuration["ApiSettings:RapidApiKey"]))
            return StartupExitCode.MissingRapidApiKey;

        var connection = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            return StartupExitCode.MissingConnectionString;

        return null;
    }
}
