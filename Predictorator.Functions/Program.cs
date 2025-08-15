using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using Serilog;
using Serilog.Events;
using Predictorator.Functions;

var builder = FunctionsApplication.CreateBuilder(args);

var logDir = Path.Combine(builder.Environment.ContentRootPath, "logs");
Directory.CreateDirectory(logDir);
var minLevel = builder.Environment.IsProduction() ? LogEventLevel.Warning : LogEventLevel.Information;
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(minLevel)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File(Path.Combine(logDir, "functions.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .WriteTo.Console()
    .WriteTo.AzureApp(restrictedToMinimumLevel: minLevel)
    .CreateLogger();

builder.Services.AddLogging(lb => lb.AddSerilog());

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

var configuration = builder.Configuration;

builder.Services.AddPredictoratorFunctionServices(configuration);

builder.Build().Run();
