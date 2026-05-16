using Microsoft.Extensions.Options;
using LegacyLens.Infrastructure.DotNetAnalysis;
using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Infrastructure.Tests.DotNetAnalysis;

public sealed class RoslynWorkspaceCacheTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"legacylens-cache-{Guid.NewGuid():N}");
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-05-16T12:00:00Z"));

    public RoslynWorkspaceCacheTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task GetOrLoadAsync_FirstRequestLoadsWorkspace()
    {
        WriteProject(_root);
        using var cache = CreateCache();

        var lease = await cache.GetOrLoadAsync(_root, CancellationToken.None);
        var status = cache.GetStatus(_root);

        Assert.False(lease.IsFromCache);
        Assert.True(lease.Workspace.Result.Success, lease.Workspace.Result.ErrorMessage);
        Assert.True(status.IsCached);
        Assert.Equal(0, status.HitCount);
    }

    [Fact]
    public async Task GetOrLoadAsync_SecondRequestReusesCache()
    {
        WriteProject(_root);
        using var cache = CreateCache();

        await cache.GetOrLoadAsync(_root, CancellationToken.None);
        var second = await cache.GetOrLoadAsync(_root, CancellationToken.None);
        var status = cache.GetStatus(_root);

        Assert.True(second.IsFromCache);
        Assert.Equal(1, status.HitCount);
    }

    [Fact]
    public async Task GetOrLoadAsync_ChangedFingerprintReloadsWorkspace()
    {
        WriteProject(_root);
        using var cache = CreateCache();
        await cache.GetOrLoadAsync(_root, CancellationToken.None);

        await Task.Delay(20);
        File.AppendAllText(Path.Combine(_root, "Code.cs"), Environment.NewLine + "public class Added { }");
        var second = await cache.GetOrLoadAsync(_root, CancellationToken.None);
        var status = cache.GetStatus(_root);

        Assert.False(second.IsFromCache);
        Assert.Equal(0, status.HitCount);
    }

    [Fact]
    public async Task GetOrLoadAsync_TtlExpiryReloadsWorkspace()
    {
        WriteProject(_root);
        using var cache = CreateCache(cacheTtlMinutes: 1);
        await cache.GetOrLoadAsync(_root, CancellationToken.None);

        _time.Advance(TimeSpan.FromMinutes(2));
        var second = await cache.GetOrLoadAsync(_root, CancellationToken.None);

        Assert.False(second.IsFromCache);
    }

    [Fact]
    public async Task GetOrLoadAsync_MaxCachedWorkspaceLimitEvictsLeastRecentlyUsed()
    {
        var repo1 = Path.Combine(_root, "repo1");
        var repo2 = Path.Combine(_root, "repo2");
        WriteProject(repo1);
        WriteProject(repo2);
        using var cache = CreateCache(maxCachedWorkspaces: 1);

        await cache.GetOrLoadAsync(repo1, CancellationToken.None);
        await cache.GetOrLoadAsync(repo2, CancellationToken.None);

        Assert.False(cache.GetStatus(repo1).IsCached);
        Assert.True(cache.GetStatus(repo2).IsCached);
    }

    [Fact]
    public async Task Invalidate_RemovesEntry()
    {
        WriteProject(_root);
        using var cache = CreateCache();
        await cache.GetOrLoadAsync(_root, CancellationToken.None);

        cache.Invalidate(_root);

        Assert.False(cache.GetStatus(_root).IsCached);
    }

    [Fact]
    public async Task FailedWorkspaceLoad_IsNotCachedForever()
    {
        using var cache = CreateCache();

        var failed = await cache.GetOrLoadAsync(_root, CancellationToken.None);
        Assert.False(cache.GetStatus(_root).IsCached);

        WriteProject(_root);
        var recovered = await cache.GetOrLoadAsync(_root, CancellationToken.None);

        Assert.False(failed.Workspace.Result.Success);
        Assert.True(recovered.Workspace.Result.Success, recovered.Workspace.Result.ErrorMessage);
    }

    [Fact]
    public async Task SymbolReferenceAndDiAnalysisReuseCache()
    {
        WriteProject(
            _root,
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace Sample;
            public interface IUserService { }
            public class UserService : IUserService { }
            public class Startup
            {
                public void ConfigureServices(IServiceCollection services)
                {
                    services.AddScoped<IUserService, UserService>();
                }
            }
            public class Controller
            {
                private readonly IUserService _service;
                public Controller(IUserService service) => _service = service;
            }
            """);
        using var cache = CreateCache();
        var scanner = new RoslynSymbolScanner(cache);
        var references = new RoslynReferenceAnalyzer(cache);
        var di = new RoslynDependencyInjectionAnalyzer(cache);

        var scan = await scanner.ScanAsync(_root, CancellationToken.None);
        var referenceResult = await references.FindReferencesAsync(
            new RoslynReferenceAnalysisRequest(_root, "IUserService"),
            CancellationToken.None);
        var diResult = await di.AnalyzeAsync(_root, CancellationToken.None);
        var status = cache.GetStatus(_root);

        Assert.True(scan.LoadResult.Success, scan.LoadResult.ErrorMessage);
        Assert.True(referenceResult.Success, referenceResult.ErrorMessage);
        Assert.True(diResult.Success, diResult.ErrorMessage);
        Assert.True(status.HitCount >= 2);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private RoslynWorkspaceCache CreateCache(
        int maxCachedWorkspaces = 3,
        int cacheTtlMinutes = 30)
        => new(
            new DotNetWorkspaceDiscovery(),
            new RoslynWorkspaceLoader(),
            Options.Create(new RoslynOptions
            {
                EnableWorkspaceCache = true,
                MaxCachedWorkspaces = maxCachedWorkspaces,
                CacheTtlMinutes = cacheTtlMinutes
            }),
            _time);

    private static void WriteProject(string root, string code = "namespace Sample; public class UserService { }")
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(
            Path.Combine(root, "Sample.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <FrameworkReference Include="Microsoft.AspNetCore.App" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(Path.Combine(root, "Code.cs"), code);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan value) => _utcNow += value;
    }
}
