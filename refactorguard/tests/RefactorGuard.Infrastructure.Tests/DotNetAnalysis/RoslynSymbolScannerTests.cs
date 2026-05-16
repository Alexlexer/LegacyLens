using RefactorGuard.Infrastructure.DotNetAnalysis;

namespace RefactorGuard.Infrastructure.Tests.DotNetAnalysis;

public sealed class RoslynSymbolScannerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"legacylens-roslyn-{Guid.NewGuid():N}");

    public RoslynSymbolScannerTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task ScanAsync_ExtractsBasicSymbolsFromTinyProject()
    {
        WriteProject();
        var scanner = new RoslynSymbolScanner(
            new DotNetWorkspaceDiscovery(),
            new RoslynWorkspaceLoader());

        var result = await scanner.ScanAsync(_root, CancellationToken.None);

        Assert.True(result.LoadResult.Success, result.LoadResult.ErrorMessage);
        Assert.True(result.ProjectCount >= 1);
        Assert.True(result.DocumentCount >= 1);
        Assert.Contains(result.Symbols, s => s.Kind == "namespace" && s.FullName == "Sample");
        Assert.Contains(result.Symbols, s => s.Kind == "class" && s.FullName == "Sample.UserService");
        Assert.Contains(result.Symbols, s => s.Kind == "interface" && s.FullName == "Sample.IUserService");
        Assert.Contains(result.Symbols, s => s.Kind == "record" && s.FullName == "Sample.UserDto");
        Assert.Contains(result.Symbols, s => s.Kind == "struct" && s.FullName == "Sample.UserKey");
        Assert.Contains(result.Symbols, s => s.Kind == "enum" && s.FullName == "Sample.UserKind");
        Assert.Contains(result.Symbols, s => s.Kind == "method" && s.FullName == "Sample.UserService.GetName");
        Assert.Contains(result.Symbols, s => s.Kind == "property" && s.FullName == "Sample.UserService.Name");
    }

    private void WriteProject()
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

        File.WriteAllText(
            Path.Combine(_root, "Symbols.cs"),
            """
            namespace Sample;

            public interface IUserService
            {
                string GetName();
            }

            public class UserService : IUserService
            {
                public string Name { get; set; } = "";
                public string GetName() => Name;
            }

            public record UserDto(string Name);

            public struct UserKey
            {
                public int Value { get; set; }
            }

            public enum UserKind
            {
                Admin
            }
            """);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
