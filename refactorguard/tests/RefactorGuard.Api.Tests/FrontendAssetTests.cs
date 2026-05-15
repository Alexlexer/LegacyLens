namespace RefactorGuard.Api.Tests;

public sealed class FrontendAssetTests
{
    [Fact]
    public void ViteUiProject_ContainsExpectedSourceFiles()
    {
        var root = FindRepositoryRoot();
        var uiPath = Path.Combine(root, "ui");

        Assert.True(File.Exists(Path.Combine(uiPath, "package.json")));
        Assert.True(File.Exists(Path.Combine(uiPath, "src", "main.ts")));
        Assert.True(File.Exists(Path.Combine(uiPath, "src", "styles.css")));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "ui", "package.json");
            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate refactorguard repository root.");
    }
}
