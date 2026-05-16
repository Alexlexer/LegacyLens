using LegacyLens.Infrastructure.Security;

namespace LegacyLens.Infrastructure.Tests;

public sealed class RepoPathValidatorTests : IDisposable
{
    private readonly string _root = CreateTempDirectory();

    [Fact]
    public void Validate_ReturnsFullPath_WhenPathIsUnderAllowedRoot()
    {
        var repo = Directory.CreateDirectory(Path.Combine(_root, "repo")).FullName;
        var validator = new RepoPathValidator([_root]);

        var result = validator.Validate(repo);

        Assert.Equal(Path.GetFullPath(repo), result);
    }

    [Fact]
    public void Validate_RejectsPathOutsideAllowedRoot()
    {
        var outside = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"LegacyLens-outside-{Guid.NewGuid():N}")).FullName;
        var validator = new RepoPathValidator([_root]);

        try
        {
            var exception = Assert.Throws<UnauthorizedAccessException>(() => validator.Validate(outside));
            Assert.Contains("outside", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public void Validate_RejectsWhenNoAllowedRootsAreConfigured()
    {
        var validator = new RepoPathValidator([]);

        var exception = Assert.Throws<UnauthorizedAccessException>(() => validator.Validate(_root));

        Assert.Contains("No repository roots", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        return Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"LegacyLens-{Guid.NewGuid():N}")).FullName;
    }
}
