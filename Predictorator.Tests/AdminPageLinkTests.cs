using System;
using System.IO;
using Xunit;

namespace Predictorator.Tests;

public class AdminPageLinkTests
{
    [Fact]
    public void AdminPage_Should_Not_Contain_BackgroundJob_Link()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Predictorator", "Components", "Pages", "Admin", "Index.razor"));
        var content = File.ReadAllText(path);
        Assert.DoesNotContain("/hangfire", content);
    }
}
