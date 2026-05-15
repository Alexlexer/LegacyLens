namespace RefactorGuard.Infrastructure.Security;

public sealed class RepoPathValidator(IReadOnlyCollection<string> allowedRoots) : IRepoPathValidator
{
    private readonly IReadOnlyList<string> _allowedRoots = allowedRoots
        .Where(root => !string.IsNullOrWhiteSpace(root))
        .Select(Path.GetFullPath)
        .Select(path => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        .ToList();

    public string Validate(string repoPath)
    {
        if (string.IsNullOrWhiteSpace(repoPath))
        {
            throw new UnauthorizedAccessException("Repository path is required.");
        }

        if (_allowedRoots.Count == 0)
        {
            throw new UnauthorizedAccessException("No repository roots are configured.");
        }

        var fullPath = Path.GetFullPath(repoPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (!Directory.Exists(fullPath))
        {
            throw new UnauthorizedAccessException("Repository path does not exist.");
        }

        if (!_allowedRoots.Any(root => IsUnderRoot(fullPath, root)))
        {
            throw new UnauthorizedAccessException("Repository path is outside the configured allowed roots.");
        }

        return fullPath;
    }

    private static bool IsUnderRoot(string path, string root)
    {
        return string.Equals(path, root, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
