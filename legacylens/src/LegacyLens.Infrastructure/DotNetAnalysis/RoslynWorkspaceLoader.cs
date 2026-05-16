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
            return LoadedRoslynWorkspace.Failed(new RoslynWorkspaceLoadResult(
                false,
                selected.Path,
                selected.Kind,
                0,
                0,
                [],
                discoveryResult.Warnings.Concat(selected.Warnings).Append(warning).Distinct().ToList(),
                ex.Message));
        }
    }

    private static void EnsureMSBuildRegistered()
    {
        if (MSBuildLocator.IsRegistered)
            return;

        lock (BuildLocatorLock)
        {
            if (!MSBuildLocator.IsRegistered && MSBuildLocator.CanRegister)
            {
                MSBuildLocator.RegisterDefaults();
            }
        }
    }

    internal sealed record LoadedRoslynWorkspace(
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
