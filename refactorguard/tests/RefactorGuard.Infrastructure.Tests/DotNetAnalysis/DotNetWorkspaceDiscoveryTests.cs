using RefactorGuard.Application.DotNetAnalysis;
using RefactorGuard.Infrastructure.DotNetAnalysis;

namespace RefactorGuard.Infrastructure.Tests.DotNetAnalysis;

public sealed class DotNetWorkspaceDiscoveryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"legacylens-discovery-{Guid.NewGuid():N}");
    private readonly DotNetWorkspaceDiscovery _discovery = new();

    public DotNetWorkspaceDiscoveryTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task DiscoverAsync_FindsSlnx()
    {
        Touch("App.slnx");

        var result = await _discovery.DiscoverAsync(_root, CancellationToken.None);

        Assert.Equal(DotNetWorkspaceKind.Slnx, result.Selected?.Kind);
        Assert.Contains(result.Candidates, c => c.Kind == DotNetWorkspaceKind.Slnx);
    }

    [Fact]
    public async Task DiscoverAsync_FindsSln()
    {
        Touch("App.sln");

        var result = await _discovery.DiscoverAsync(_root, CancellationToken.None);

        Assert.Equal(DotNetWorkspaceKind.Sln, result.Selected?.Kind);
    }

    [Fact]
    public async Task DiscoverAsync_FindsCsproj()
    {
        Touch("src/App/App.csproj");

        var result = await _discovery.DiscoverAsync(_root, CancellationToken.None);

        Assert.Equal(DotNetWorkspaceKind.Csproj, result.Selected?.Kind);
    }

    [Fact]
    public async Task DiscoverAsync_PrefersSlnxOverSlnAndWarns()
    {
        Touch("App.sln");
        Touch("App.slnx");

        var result = await _discovery.DiscoverAsync(_root, CancellationToken.None);

        Assert.Equal(DotNetWorkspaceKind.Slnx, result.Selected?.Kind);
        Assert.Contains("Both .slnx and .sln files were found", string.Join("\n", result.Warnings));
    }

    [Fact]
    public async Task DiscoverAsync_ExcludesGeneratedAndToolDirectories()
    {
        Touch("bin/Bin.csproj");
        Touch("obj/Obj.csproj");
        Touch(".git/Git.csproj");
        Touch("node_modules/Node.csproj");
        Touch(".vs/Vs.csproj");
        Touch("packages/Package.csproj");
        Touch("src/App/App.csproj");

        var result = await _discovery.DiscoverAsync(_root, CancellationToken.None);

        Assert.Single(result.Candidates);
        Assert.EndsWith(Path.Combine("src", "App", "App.csproj"), result.Selected!.Path);
    }

    [Fact]
    public async Task DiscoverAsync_SelectsShallowestRootMostCandidate()
    {
        Touch("src/deep/Deep.sln");
        Touch("Root.sln");

        var result = await _discovery.DiscoverAsync(_root, CancellationToken.None);

        Assert.EndsWith("Root.sln", result.Selected!.Path);
    }

    private void Touch(string relativePath)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, string.Empty);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
