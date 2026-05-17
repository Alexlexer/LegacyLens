using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Infrastructure.DotNetAnalysis;

public sealed class RoslynWorkspaceLoader : IRoslynWorkspaceLoader
{
    private static readonly object BuildLocatorLock = new();

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
            var msbuildInfo = RegisteredMSBuildPath != null ? $" [MSBuild: {RegisteredMSBuildPath}]" : "";
            var detail = BuildExceptionChain(ex) + msbuildInfo;
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

    private static string BuildExceptionChain(Exception ex)
    {
        // Walk InnerException chain for the normal hierarchy
        var parts = new System.Text.StringBuilder();
        var current = ex;
        while (current != null)
        {
            if (parts.Length > 0) parts.Append(" → ");
            parts.Append(current.GetType().Name).Append(": ").Append(current.Message);
            current = current.InnerException;
        }
        // RemoteInvocationException wraps the remote exception as text in its Data or in a
        // nested property — fall back to the full ToString() to capture it.
        if (ex.InnerException == null && ex.ToString().Length > parts.Length + 20)
        {
            var full = ex.ToString();
            var lines = full.Split('\n');
            // First 6 lines usually contain the full remote chain without huge stack frames.
            parts.Append(" | ").Append(string.Join(" | ", lines.Take(6).Select(l => l.Trim()).Where(l => l.Length > 0)));
        }
        return parts.ToString();
    }

    private static string? _registeredMSBuildPath;

    internal static string? RegisteredMSBuildPath => _registeredMSBuildPath;

    private static void EnsureMSBuildRegistered()
    {
        if (MSBuildLocator.IsRegistered)
            return;

        lock (BuildLocatorLock)
        {
            if (MSBuildLocator.IsRegistered || !MSBuildLocator.CanRegister)
                return;

            // .NET SDK 9.x (MSBuild 17.x) is compatible with Roslyn 5.3.0's BuildHost
            // which ships System.Collections.Immutable 9.0.0. SDK 10+ ships MSBuild 18.x
            // which requires SCI 10.0.0.3 — loading it causes XMakeElements TypeInitializationException
            // from both BuildHost-net472 (binding redirect caps at 9.x) and BuildHost-netcore
            // (deps.json lists SCI 9.0.0 which can conflict). VS 2026's MSBuild is also 18.x.
            var sdkInstance = MSBuildLocator.QueryVisualStudioInstances()
                .Where(vs => vs.DiscoveryType == DiscoveryType.DotNetSdk && vs.Version.Major < 10)
                .OrderByDescending(vs => vs.Version)
                .FirstOrDefault();

            if (sdkInstance != null)
            {
                _registeredMSBuildPath = $"SDK {sdkInstance.Version} @ {sdkInstance.MSBuildPath}";
                MSBuildLocator.RegisterInstance(sdkInstance);
                return;
            }

            // VS 2022 (v17.x) works with BuildHost-net472 + SCI v9. VS 2026 (v18.x)
            // is intentionally excluded for the reason above.
            var vs2022Candidates = new[]
            {
                @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin",
                @"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin",
                @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin",
            };

            foreach (var path in vs2022Candidates)
            {
                if (Directory.Exists(path))
                {
                    _registeredMSBuildPath = $"VS 2022 direct @ {path}";
                    MSBuildLocator.RegisterMSBuildPath(path);
                    return;
                }
            }

            _registeredMSBuildPath = "SDK defaults";
            MSBuildLocator.RegisterDefaults();
        }
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
