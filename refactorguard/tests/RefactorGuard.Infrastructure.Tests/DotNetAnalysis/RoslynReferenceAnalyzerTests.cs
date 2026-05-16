using RefactorGuard.Application.DotNetAnalysis;
using RefactorGuard.Infrastructure.DotNetAnalysis;

namespace RefactorGuard.Infrastructure.Tests.DotNetAnalysis;

public sealed class RoslynReferenceAnalyzerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"legacylens-refs-{Guid.NewGuid():N}");

    public RoslynReferenceAnalyzerTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task FindReferencesAsync_FindsReferencesToClass()
    {
        WriteProject(
            """
            namespace Sample;

            public class UserService
            {
                public string Name => "";
            }

            public class UserController
            {
                private readonly UserService _service = new UserService();
            }
            """);
        var analyzer = CreateAnalyzer();

        var result = await analyzer.FindReferencesAsync(
            new RoslynReferenceAnalysisRequest(_root, "UserService"),
            CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Single(result.MatchedSymbols);
        Assert.Contains(result.References, r => r.IsDefinition && r.SymbolFullName == "Sample.UserService");
        Assert.Contains(result.References, r => !r.IsDefinition && r.FilePath.EndsWith("Code.cs"));
    }

    [Fact]
    public async Task FindReferencesAsync_FindsReferencesToInterface()
    {
        WriteProject(
            """
            namespace Sample;

            public interface IUserService
            {
                string Name { get; }
            }

            public class UserService : IUserService
            {
                public string Name => "";
            }

            public class UserController
            {
                private IUserService _service;
            }
            """);
        var analyzer = CreateAnalyzer();

        var result = await analyzer.FindReferencesAsync(
            new RoslynReferenceAnalysisRequest(_root, "IUserService"),
            CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Single(result.MatchedSymbols);
        Assert.Contains(result.References, r => !r.IsDefinition && r.SymbolKind == "interface");
    }

    [Fact]
    public async Task FindReferencesAsync_ReturnsWarningForDuplicateSymbolNames()
    {
        WriteProject(
            """
            namespace First;
            public class Duplicate { }

            namespace Second;
            public class Duplicate { }
            """);
        var analyzer = CreateAnalyzer();

        var result = await analyzer.FindReferencesAsync(
            new RoslynReferenceAnalysisRequest(_root, "Duplicate"),
            CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(2, result.MatchedSymbols.Count);
        Assert.Contains("Multiple symbols matched", string.Join("\n", result.Warnings));
    }

    [Fact]
    public async Task FindReferencesAsync_ReturnsEmptyResultWhenNoSymbolMatches()
    {
        WriteProject("namespace Sample; public class UserService { }");
        var analyzer = CreateAnalyzer();

        var result = await analyzer.FindReferencesAsync(
            new RoslynReferenceAnalysisRequest(_root, "MissingSymbol"),
            CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Empty(result.MatchedSymbols);
        Assert.Empty(result.References);
    }

    [Fact]
    public async Task FindReferencesAsync_SkipsMetadataOnlyReferences()
    {
        WriteProject("namespace Sample; public class UsesString { public string Name => string.Empty; }");
        var analyzer = CreateAnalyzer();

        var result = await analyzer.FindReferencesAsync(
            new RoslynReferenceAnalysisRequest(_root, "String"),
            CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Empty(result.MatchedSymbols);
        Assert.Empty(result.References);
    }

    [Fact]
    public async Task FindReferencesAsync_DoesNotCrashWhenWorkspaceCannotLoad()
    {
        var analyzer = CreateAnalyzer();

        var result = await analyzer.FindReferencesAsync(
            new RoslynReferenceAnalysisRequest(_root, "Anything"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task FindReferencesAsync_RespectsMaxResults()
    {
        WriteProject(
            """
            namespace Sample;

            public class UserService { }

            public class Uses
            {
                public void M()
                {
                    _ = new UserService();
                    _ = new UserService();
                    _ = new UserService();
                    _ = new UserService();
                }
            }
            """);
        var analyzer = CreateAnalyzer();

        var result = await analyzer.FindReferencesAsync(
            new RoslynReferenceAnalysisRequest(_root, "UserService", MaxResults: 3),
            CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.True(result.References.Count <= 3);
    }

    private RoslynReferenceAnalyzer CreateAnalyzer()
        => new(new DotNetWorkspaceDiscovery(), new RoslynWorkspaceLoader());

    private void WriteProject(string code)
    {
        File.WriteAllText(
            Path.Combine(_root, "Sample.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        File.WriteAllText(Path.Combine(_root, "Code.cs"), code);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
