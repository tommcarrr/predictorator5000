using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Predictorator.Tests.Helpers;

public class FakeHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "Test";
    public string ContentRootPath { get; set; } = ".";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
