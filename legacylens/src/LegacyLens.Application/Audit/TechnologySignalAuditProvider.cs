using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Application.Audit;

public sealed class TechnologySignalAuditProvider : IAuditProvider
{
    public string Name => "TechnologySignal";

    public Task<AuditProviderResult> AnalyzeAsync(
        AuditContext context,
        CancellationToken cancellationToken)
    {
        var signals = new List<TechnologySignal>();
        var findings = new List<AuditFinding>();

        CollectFileBasedSignals(context.RepoPath, signals, findings);
        CollectWorkspaceSignals(context.WorkspaceDiscovery, signals, findings);

        return Task.FromResult(new AuditProviderResult(
            Name,
            TechnologySignals: signals,
            RiskFindings: findings));
    }

    private static void CollectFileBasedSignals(
        string repoPath,
        List<TechnologySignal> signals,
        List<AuditFinding> findings)
    {
        if (!Directory.Exists(repoPath))
            return;

        var fileChecks = new (string FileName, string SignalName, string Category, string FindingCode, string FindingSeverity)[]
        {
            ("web.config", ".NET Framework web.config", "Framework", "web-config-present", "Warning"),
            ("packages.config", "packages.config (NuGet v2)", "Dependencies", "packages-config-present", "Warning"),
            ("Global.asax", "ASP.NET Global.asax", "Framework", string.Empty, string.Empty),
            ("App.config", "App.config", "Configuration", string.Empty, string.Empty),
            ("appsettings.json", "appsettings.json (ASP.NET Core style)", "Configuration", string.Empty, string.Empty),
        };

        var foundFiles = AuditFileSystemHelpers.EnumerateFiles(repoPath);

        foreach (var (fileName, signalName, category, findingCode, findingSeverity) in fileChecks)
        {
            var match = foundFiles.FirstOrDefault(f =>
                string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));

            if (match is null)
                continue;

            var relative = Path.GetRelativePath(repoPath, match);
            signals.Add(new TechnologySignal(signalName, category, $"File found: {relative}", relative, "high"));

            if (!string.IsNullOrEmpty(findingCode))
            {
                findings.Add(new AuditFinding(
                    findingSeverity,
                    findingCode,
                    signalName,
                    $"{fileName} was found at {relative}. This is characteristic of legacy .NET projects.",
                    relative));
            }
        }

        var appStartDirs = AuditFileSystemHelpers.FindDirectories(repoPath, "App_Start");
        if (appStartDirs.Count > 0)
        {
            var relative = Path.GetRelativePath(repoPath, appStartDirs[0]);
            signals.Add(new TechnologySignal(
                "ASP.NET MVC App_Start",
                "Framework",
                $"App_Start directory found: {relative}",
                relative,
                "high"));
        }

        if (AuditFileSystemHelpers.HasTestProjects(repoPath))
        {
            signals.Add(new TechnologySignal(
                "Tests present",
                "Quality",
                "Test project or test file detected.",
                null,
                "high"));
        }
        else
        {
            signals.Add(new TechnologySignal(
                "No tests detected",
                "Quality",
                "No test project or test file detected.",
                null,
                "medium"));
            findings.Add(new AuditFinding(
                "Warning",
                "no-tests-detected",
                "No test projects detected",
                "No test files or test projects were found. Adding tests before modernization reduces regression risk."));
        }
    }

    private static void CollectWorkspaceSignals(
        DotNetWorkspaceDiscoveryResult discovery,
        List<TechnologySignal> signals,
        List<AuditFinding> findings)
    {
        var slnxCount = discovery.Candidates.Count(c => c.Kind == DotNetWorkspaceKind.Slnx);
        var slnCount = discovery.Candidates.Count(c => c.Kind == DotNetWorkspaceKind.Sln);
        var csprojCount = discovery.Candidates.Count(c => c.Kind == DotNetWorkspaceKind.Csproj);

        if (discovery.Selected is null)
        {
            findings.Add(new AuditFinding(
                "Warning",
                "missing-solution-file",
                "No solution or project file found",
                "No .slnx, .sln, or .csproj was found. Workspace-based analysis was skipped."));
            return;
        }

        if (slnxCount > 0 && slnCount > 0)
        {
            signals.Add(new TechnologySignal(
                "Both .sln and .slnx present",
                "Workspace",
                $".slnx ({slnxCount}) and .sln ({slnCount}) files coexist.",
                null,
                "high"));
            findings.Add(new AuditFinding(
                "Info",
                "sln-and-slnx-both-present",
                "Both .sln and .slnx files present",
                "Both .sln and .slnx files were found. Consider consolidating to a single format."));
        }

        if (slnCount > 1)
        {
            signals.Add(new TechnologySignal(
                "Multiple .sln files",
                "Workspace",
                $"{slnCount} .sln files found.",
                null,
                "medium"));
            findings.Add(new AuditFinding(
                "Info",
                "multiple-solution-files",
                "Multiple solution files found",
                $"Found {slnCount} .sln files. Multiple solutions can indicate a large or modularized codebase."));
        }

        if (csprojCount > 0 && slnCount == 0 && slnxCount == 0)
        {
            signals.Add(new TechnologySignal(
                "No solution file (csproj only)",
                "Workspace",
                $"{csprojCount} .csproj file(s) found without a .sln or .slnx.",
                null,
                "medium"));
        }
    }
}
