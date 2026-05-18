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

            // Prefer a VS installation: it ships .NET Framework MSBuild which the
            // BuildHost-net472 (required for .NET Framework solutions) can load.
            // MSBuildLocator 1.11.2's QueryVisualStudioInstances() may not recognize VS 2026
            // (DiscoveryType.VisualStudioSetup returns nothing), so we probe known paths first.
            var knownVsMSBuildPaths = new[]
            {
                @"C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin",
                @"C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin",
                @"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin",
                @"C:\Program Files\Microsoft Visual Studio\17\Enterprise\MSBuild\Current\Bin",
                @"C:\Program Files\Microsoft Visual Studio\17\Professional\MSBuild\Current\Bin",
                @"C:\Program Files\Microsoft Visual Studio\17\Community\MSBuild\Current\Bin",
            };

            var vsPath = knownVsMSBuildPaths.FirstOrDefault(System.IO.Directory.Exists);
            if (vsPath != null)
            {
                // VS MSBuild is .NET Framework only — BuildHost-netcore needs DOTNET_HOST_PATH
                // to launch via dotnet.exe. RegisterMSBuildPath doesn't set this for VS instances.
                var dotnetExe = @"C:\Program Files\dotnet\dotnet.exe";
                if (System.IO.File.Exists(dotnetExe) &&
                    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")))
                {
                    Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", dotnetExe);
                }

                _registeredMSBuildPath = $"VS @ {vsPath}";
                MSBuildLocator.RegisterMSBuildPath(vsPath);
                return;
            }

            // Fall back to MSBuildLocator's VS discovery (covers non-standard install paths).
            var vsInstance = MSBuildLocator.QueryVisualStudioInstances()
                .Where(vs => vs.DiscoveryType == DiscoveryType.VisualStudioSetup)
                .OrderByDescending(vs => vs.Version)
                .FirstOrDefault();

            if (vsInstance != null)
            {
                _registeredMSBuildPath = $"VS {vsInstance.Version} @ {vsInstance.MSBuildPath}";
                MSBuildLocator.RegisterInstance(vsInstance);
                return;
            }

            // No VS installation found — fall back to SDK 9.x (CoreCLR MSBuild).
            // This works for SDK-style projects via BuildHost-netcore but will fail
            // for .NET Framework solutions that require BuildHost-net472.
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
