namespace Predictorator.Tests;

public class RenderModeTests
{
    private static string GetRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "Predictorator.sln")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        if (dir == null) throw new InvalidOperationException("Repo root not found");
        return dir;
    }

    [Fact]
    public void Pages_Should_Declare_RenderMode()
    {
        var root = GetRepoRoot();
        var pagesDir = Path.Combine(root, "Predictorator", "Components", "Pages");
        foreach (var file in Directory.GetFiles(pagesDir, "*.razor", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            Assert.Contains("@rendermode InteractiveServer", content, StringComparison.Ordinal);
        }

        var appPath = Path.Combine(root, "Predictorator", "Components", "App.razor");
        var appContent = File.ReadAllText(appPath);
        Assert.DoesNotContain("@rendermode InteractiveServer", appContent, StringComparison.Ordinal);
    }
}
