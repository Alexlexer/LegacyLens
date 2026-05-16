using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Infrastructure.DotNetAnalysis;

public sealed class DotNetWorkspaceDiscovery : IDotNetWorkspaceDiscovery
{
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".vs",
        "bin",
        "obj",
        "node_modules",
        "packages"
    };

    public Task<DotNetWorkspaceDiscoveryResult> DiscoverAsync(
        string repoRoot,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
            throw new ArgumentException("Repository path is required.", nameof(repoRoot));

        var root = Path.GetFullPath(repoRoot);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Repository path '{root}' does not exist.");

        var files = EnumerateWorkspaceFiles(root, cancellationToken).ToList();
        var slnx = files.Where(path => path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)).ToList();
        var sln = files.Where(path => path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)).ToList();
        var csproj = files.Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).ToList();
        var warnings = new List<string>();

        if (slnx.Count > 0 && sln.Count > 0)
        {
            warnings.Add("Both .slnx and .sln files were found. LegacyLens selected .slnx by default.");
        }

        var selectedPath = SelectDefault(root, slnx.Count > 0 ? slnx : sln.Count > 0 ? sln : csproj);
        var candidates = slnx.Select(path => ToCandidate(path, DotNetWorkspaceKind.Slnx))
            .Concat(sln.Select(path => ToCandidate(path, DotNetWorkspaceKind.Sln)))
            .Concat(csproj.Select(path => ToCandidate(path, DotNetWorkspaceKind.Csproj)))
            .OrderBy(candidate => candidate.Kind)
            .ThenBy(candidate => Depth(root, candidate.Path))
            .ThenBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(new DotNetWorkspaceDiscoveryResult(
            candidates,
            candidates.FirstOrDefault(candidate => candidate.IsSelected),
            warnings));

        DotNetWorkspaceCandidate ToCandidate(string path, DotNetWorkspaceKind kind)
        {
            var isSelected = string.Equals(path, selectedPath, StringComparison.OrdinalIgnoreCase);
            return new DotNetWorkspaceCandidate(path, kind, isSelected, isSelected ? warnings : []);
        }
    }

    private static IEnumerable<string> EnumerateWorkspaceFiles(
        string root,
        CancellationToken cancellationToken)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = pending.Pop();

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(current)
                    .Where(path =>
                        path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
                yield return Path.GetFullPath(file);

            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(current)
                    .Where(path => !ExcludedDirectories.Contains(Path.GetFileName(path)))
                    .ToList();
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var directory in directories)
                pending.Push(directory);
        }
    }

    private static string? SelectDefault(string root, IReadOnlyList<string> paths)
    {
        return paths
            .OrderBy(path => Depth(root, path))
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static int Depth(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        return relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
