using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Infrastructure.DotNetAnalysis;

public sealed class RoslynWorkspaceLoader : IRoslynWorkspaceLoader
{
    private static readonly object BuildLocatorLock = new();
    private static string? _registeredMSBuildPath;

    internal static string? RegisteredMSBuildPath => _registeredMSBuildPath;

    public async Task<RoslynWorkspaceLoadResult> LoadAsync(
        DotNetWorkspaceDiscoveryResult discoveryResult,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadWorkspaceForScanAsync(discoveryResult, cancellationToken);
        using (loaded)
        {
            return loaded.Result;
        }
    }

    internal async Task<LoadedRoslynWorkspace> LoadWorkspaceForScanAsync(
        DotNetWorkspaceDiscoveryResult discoveryResult,
        CancellationToken cancellationToken)
    {
        var selected = discoveryResult.Selected;
        if (selected is null)
        {
            return LoadedRoslynWorkspace.Failed(new RoslynWorkspaceLoadResult(
                false,
                null,
                null,
                0,
                0,
                [],
                discoveryResult.Warnings,
                "No .slnx, .sln, or .csproj files were found."));
        }

        try
        {
            EnsureMSBuildRegistered();
            var workspace = MSBuildWorkspace.Create();
            workspace.RegisterWorkspaceFailedHandler(_ => { });

            Solution solution;
            if (selected.Kind is DotNetWorkspaceKind.Slnx or DotNetWorkspaceKind.Sln)
            {
                solution = await workspace.OpenSolutionAsync(selected.Path, cancellationToken: cancellationToken);
            }
            else
            {
                foreach (var candidate in discoveryResult.Candidates.Where(c => c.Kind == DotNetWorkspaceKind.Csproj))
                {
                    await workspace.OpenProjectAsync(candidate.Path, cancellationToken: cancellationToken);
                }

                solution = workspace.CurrentSolution;
            }

            var projects = solution.Projects.Where(project => project.Language == LanguageNames.CSharp).ToList();
            var diagnostics = workspace.Diagnostics.Select(d => d.Message).ToList();
            var documentCount = projects.Sum(project => project.Documents.Count(document => document.SourceCodeKind == SourceCodeKind.Regular));

            return new LoadedRoslynWorkspace(
                workspace,
                solution,
                projects,
                new RoslynWorkspaceLoadResult(
                    true,
                    selected.Path,
                    selected.Kind,
                    projects.Count,
                    documentCount,
                    diagnostics,
                    discoveryResult.Warnings.Concat(selected.Warnings).Distinct().ToList(),
                    null));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var warning = selected.Kind == DotNetWorkspaceKind.Slnx
                ? "If .slnx loading is not supported by the installed SDK/MSBuildWorkspace combination, use a .sln or .csproj fallback until Roslyn/MSBuild support is available."
                : "Roslyn workspace loading failed. Ensure the local .NET SDK/MSBuild installation can evaluate the selected workspace.";
            var detail = BuildExceptionDetail(ex);
            return LoadedRoslynWorkspace.Failed(new RoslynWorkspaceLoadResult(
                false,
                selected.Path,
                selected.Kind,
                0,
                0,
                [],
                discoveryResult.Warnings.Concat(selected.Warnings).Append(warning).Distinct().ToList(),
                detail));
        }
    }

    private static string BuildExceptionDetail(Exception exception)
    {
        var parts = new List<string>();
        var current = exception;
        while (current is not null)
        {
            parts.Add($"{current.GetType().Name}: {current.Message}");
            current = current.InnerException;
        }

        var detail = string.Join(" → ", parts);

        // Some MSBuild/Roslyn failures cross process boundaries and only preserve the
        // remote exception chain in ToString(). Keep a small bounded prefix so users
        // can see the real loader error without dumping a huge stack trace into reports.
        if (exception.InnerException is null)
        {
            var full = exception.ToString();
            if (full.Length > detail.Length + 20)
            {
                var remotePrefix = string.Join(
                    " | ",
                    full.Split('\n')
                        .Select(line => line.Trim())
                        .Where(line => line.Length > 0)
                        .Take(6));
                if (!string.IsNullOrWhiteSpace(remotePrefix))
                    detail = $"{detail} | {remotePrefix}";
            }
        }

        if (!string.IsNullOrWhiteSpace(RegisteredMSBuildPath))
            detail = $"{detail} [MSBuild: {RegisteredMSBuildPath}]";

        return detail;
    }

    private static void EnsureMSBuildRegistered()
    {
        if (MSBuildLocator.IsRegistered)
            return;

        lock (BuildLocatorLock)
        {
            if (MSBuildLocator.IsRegistered || !MSBuildLocator.CanRegister)
                return;

            var visualStudioInstance = MSBuildLocator.QueryVisualStudioInstances()
                .Where(IsVisualStudioInstance)
                .OrderByDescending(instance => instance.Version)
                .FirstOrDefault();

            if (visualStudioInstance is not null)
            {
                _registeredMSBuildPath = $"Visual Studio {visualStudioInstance.Version} @ {visualStudioInstance.MSBuildPath}";
                MSBuildLocator.RegisterInstance(visualStudioInstance);
                return;
            }

            var directMsBuildPath = FindVisualStudioMSBuildPath();
            if (directMsBuildPath is not null)
            {
                _registeredMSBuildPath = $"Visual Studio MSBuild @ {directMsBuildPath}";
                MSBuildLocator.RegisterMSBuildPath(directMsBuildPath);
                return;
            }

            _registeredMSBuildPath = "MSBuildLocator.RegisterDefaults";
            MSBuildLocator.RegisterDefaults();
        }
    }

    private static bool IsVisualStudioInstance(VisualStudioInstance instance)
    {
        if (instance.Name.Contains("Visual Studio", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(instance.VisualStudioRootPath))
            return false;

        return Directory.Exists(Path.Combine(instance.VisualStudioRootPath, "Common7", "IDE"));
    }

    private static string? FindVisualStudioMSBuildPath()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };
        var versions = new[] { "18", "2026", "2022", "2019" };
        var editions = new[] { "Enterprise", "Professional", "Community", "BuildTools" };

        foreach (var root in roots.Where(root => !string.IsNullOrWhiteSpace(root)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var version in versions)
            {
                foreach (var edition in editions)
                {
                    var candidate = Path.Combine(
                        root,
                        "Microsoft Visual Studio",
                        version,
                        edition,
                        "MSBuild",
                        "Current",
                        "Bin");
                    if (Directory.Exists(candidate))
                        return candidate;
                }
            }
        }

        return null;
    }

    public sealed record LoadedRoslynWorkspace(
        Workspace? Workspace,
        Solution? Solution,
        IReadOnlyList<Project> Projects,
        RoslynWorkspaceLoadResult Result) : IDisposable
    {
        public static LoadedRoslynWorkspace Failed(RoslynWorkspaceLoadResult result)
            => new(null, null, [], result);

        public void Dispose()
        {
            Workspace?.Dispose();
        }
    }
}

