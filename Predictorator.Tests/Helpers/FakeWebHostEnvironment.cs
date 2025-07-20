using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Predictorator.Tests.Helpers;

public class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = ".";
    public string ContentRootPath { get; set; } = ".";
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "Test";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
