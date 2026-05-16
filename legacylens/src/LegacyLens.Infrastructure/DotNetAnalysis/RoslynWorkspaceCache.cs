using Microsoft.Extensions.Options;
using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Infrastructure.DotNetAnalysis;

public sealed class RoslynWorkspaceCache(
    IDotNetWorkspaceDiscovery discovery,
    RoslynWorkspaceLoader workspaceLoader,
    IOptions<RoslynOptions> options,
    TimeProvider timeProvider) : IRoslynWorkspaceCache, IDisposable
{
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", "node_modules", "packages", ".vs"
    };

    private readonly Dictionary<string, CacheEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<RoslynWorkspaceCacheLease> GetOrLoadAsync(
        string repoPath,
        CancellationToken cancellationToken)
    {
        var normalizedRepoPath = Path.GetFullPath(repoPath);
        var discoveryResult = await discovery.DiscoverAsync(normalizedRepoPath, cancellationToken);
        var fingerprint = BuildFingerprint(normalizedRepoPath, discoveryResult);

        if (!options.Value.EnableWorkspaceCache)
        {
            var uncached = await workspaceLoader.LoadWorkspaceForScanAsync(discoveryResult, cancellationToken);
            return new RoslynWorkspaceCacheLease(discoveryResult, uncached, IsFromCache: false, fingerprint, DisposeOnRelease: true);
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            if (_entries.TryGetValue(normalizedRepoPath, out var entry) &&
                entry.SourceFingerprint == fingerprint &&
                now - entry.LoadedAtUtc <= TimeSpan.FromMinutes(options.Value.CacheTtlMinutes))
            {
                entry.LastAccessedUtc = now;
                entry.HitCount++;
                return new RoslynWorkspaceCacheLease(discoveryResult, entry.Workspace, IsFromCache: true, fingerprint);
            }

            RemoveEntry(normalizedRepoPath);
        }
        finally
        {
            _gate.Release();
        }

        var loaded = await workspaceLoader.LoadWorkspaceForScanAsync(discoveryResult, cancellationToken);

        if (!loaded.Result.Success)
            return new RoslynWorkspaceCacheLease(discoveryResult, loaded, IsFromCache: false, fingerprint, DisposeOnRelease: true);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            _entries[normalizedRepoPath] = new CacheEntry(
                normalizedRepoPath,
                fingerprint,
                loaded,
                now,
                now);
            EvictIfNeeded();
        }
        catch
        {
            loaded.Dispose();
            throw;
        }
        finally
        {
            _gate.Release();
        }

        return new RoslynWorkspaceCacheLease(discoveryResult, loaded, IsFromCache: false, fingerprint);
    }

    public RoslynWorkspaceCacheStatus GetStatus(string repoPath)
    {
        var normalizedRepoPath = Path.GetFullPath(repoPath);
        _gate.Wait();
        try
        {
            if (!_entries.TryGetValue(normalizedRepoPath, out var entry))
            {
                return new RoslynWorkspaceCacheStatus(
                    normalizedRepoPath,
                    false,
                    null,
                    null,
                    null,
                    null,
                    0,
                    0,
                    [],
                    [],
                    null,
                    0);
            }

            return new RoslynWorkspaceCacheStatus(
                entry.RepoPath,
                true,
                entry.Workspace.Result.WorkspacePath,
                entry.Workspace.Result.WorkspaceKind,
                entry.LoadedAtUtc,
                entry.LastAccessedUtc,
                entry.Workspace.Result.ProjectCount,
                entry.Workspace.Result.DocumentCount,
                entry.Workspace.Result.Diagnostics,
                entry.Workspace.Result.Warnings,
                entry.SourceFingerprint,
                entry.HitCount);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate(string repoPath)
    {
        var normalizedRepoPath = Path.GetFullPath(repoPath);
        _gate.Wait();
        try
        {
            RemoveEntry(normalizedRepoPath);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Clear()
    {
        _gate.Wait();
        try
        {
            foreach (var entry in _entries.Values)
                entry.Workspace.Dispose();
            _entries.Clear();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        Clear();
        _gate.Dispose();
    }

    private static string BuildFingerprint(
        string repoPath,
        DotNetWorkspaceDiscoveryResult discoveryResult)
    {
        var selected = discoveryResult.Selected;
        var workspacePath = selected?.Path ?? string.Empty;
        var workspaceTicks = LastWriteTicks(workspacePath);
        var projectTicks = discoveryResult.Candidates
            .Where(c => c.Kind == DotNetWorkspaceKind.Csproj)
            .Select(c => LastWriteTicks(c.Path))
            .DefaultIfEmpty(0)
            .Max();

        var csFileCount = 0;
        long maxCsTicks = 0;
        foreach (var file in EnumerateSourceFiles(repoPath, "*.cs"))
        {
            csFileCount++;
            maxCsTicks = Math.Max(maxCsTicks, LastWriteTicks(file));
        }

        var configTicks = EnumerateConfigFiles(repoPath)
            .Select(LastWriteTicks)
            .DefaultIfEmpty(0)
            .Max();

        return string.Join(
            "|",
            workspacePath,
            selected?.Kind.ToString() ?? "none",
            workspaceTicks,
            projectTicks,
            csFileCount,
            maxCsTicks,
            configTicks);
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root, string pattern)
    {
        if (!Directory.Exists(root))
            return [];

        return Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories)
            .Where(path => !HasExcludedSegment(root, path));
    }

    private static IEnumerable<string> EnumerateConfigFiles(string root)
    {
        if (!Directory.Exists(root))
            return [];

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "appsettings.json", "web.config", "app.config", "packages.config", "global.json"
        };

        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => !HasExcludedSegment(root, path))
            .Where(path => names.Contains(Path.GetFileName(path)));
    }

    private static bool HasExcludedSegment(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        return relative
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => ExcludedDirectories.Contains(segment));
    }

    private static long LastWriteTicks(string? path)
        => !string.IsNullOrWhiteSpace(path) && File.Exists(path)
            ? File.GetLastWriteTimeUtc(path).Ticks
            : 0;

    private void EvictIfNeeded()
    {
        while (_entries.Count > options.Value.MaxCachedWorkspaces)
        {
            var oldest = _entries.Values
                .OrderBy(entry => entry.LastAccessedUtc)
                .First();
            RemoveEntry(oldest.RepoPath);
        }
    }

    private void RemoveEntry(string repoPath)
    {
        if (!_entries.Remove(repoPath, out var entry))
            return;

        entry.Workspace.Dispose();
    }

    private sealed class CacheEntry(
        string repoPath,
        string sourceFingerprint,
        RoslynWorkspaceLoader.LoadedRoslynWorkspace workspace,
        DateTimeOffset loadedAtUtc,
        DateTimeOffset lastAccessedUtc)
    {
        public string RepoPath { get; } = repoPath;
        public string SourceFingerprint { get; } = sourceFingerprint;
        public RoslynWorkspaceLoader.LoadedRoslynWorkspace Workspace { get; } = workspace;
        public DateTimeOffset LoadedAtUtc { get; } = loadedAtUtc;
        public DateTimeOffset LastAccessedUtc { get; set; } = lastAccessedUtc;
        public long HitCount { get; set; }
    }
}
