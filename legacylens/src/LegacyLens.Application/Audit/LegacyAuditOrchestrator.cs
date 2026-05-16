using System.Net;
using System.Text;
using LegacyLens.Application.DotNetAnalysis;
using LegacyLens.Application.Review;
using LegacyLens.Application.Search;
using LegacyLens.Domain.Search;

namespace LegacyLens.Application.Audit;

public sealed class LegacyAuditOrchestrator(
    IDotNetWorkspaceDiscovery workspaceDiscovery,
    IRoslynWorkspaceLoader workspaceLoader,
    IRoslynSymbolScanner symbolScanner,
    IRoslynDependencyInjectionAnalyzer diAnalyzer,
    IGpuSearchClient gpuSearchClient,
    IReviewLlmProvider llmProvider,
    ILegacyAuditMarkdownFormatter markdownFormatter) : ILegacyAuditOrchestrator
{
    private const int MaxGpuSearchResultsPerQuery = 5;
    private const int MaxTotalGpuSearchResults = 50;

    private static readonly IReadOnlyList<string> GpuSearchAuditQueries =
    [
        "web.config",
        "Global.asax",
        "packages.config",
        "System.Web",
        "System.Web.Mvc",
        "App_Start",
        "SqlConnection",
        "ExecuteSql",
        "FromSql",
        "catch (Exception)",
        ".Result",
        ".Wait()",
        "GetService",
        "GetRequiredService",
        "AddSingleton",
        "AddScoped",
        "AddTransient"
    ];

    public async Task<LegacyAuditReport> AuditAsync(
        LegacyAuditRequest request,
        CancellationToken cancellationToken)
    {
        var repoPath = request.RepoPath ?? string.Empty;
        var findings = new List<AuditFinding>();
        var technologySignals = new List<TechnologySignal>();
        var architectureSignals = new List<ArchitectureSignal>();

        var discoveryResult = await workspaceDiscovery.DiscoverAsync(repoPath, cancellationToken);
        var workspaceSummary = BuildWorkspaceSummary(discoveryResult);

        CollectFileBasedSignals(repoPath, technologySignals, findings);
        CollectWorkspaceSignals(discoveryResult, technologySignals, findings);

        AuditRoslynSummary? roslynSummary = null;
        if (request.IncludeRoslyn)
        {
            roslynSummary = await TryBuildRoslynSummaryAsync(
                repoPath, discoveryResult, findings, cancellationToken);
        }

        AuditDependencyInjectionSummary? diSummary = null;
        if (request.IncludeDependencyInjection)
        {
            diSummary = await TryBuildDiSummaryAsync(repoPath, findings, cancellationToken);
        }

        if (diSummary is not null)
        {
            CollectDiSignals(diSummary, technologySignals, findings);
        }

        AuditGpuSearchSummary? gpuSearchSummary = null;
        if (request.IncludeGpuSearch)
        {
            gpuSearchSummary = await TryBuildGpuSearchSummaryAsync(
                repoPath, request.IncludeDotNetPresets, technologySignals, findings, cancellationToken);
        }

        CollectArchitectureSignals(technologySignals, roslynSummary, diSummary, architectureSignals);

        var nextSteps = BuildRecommendedNextSteps(technologySignals, findings, roslynSummary, diSummary);
        var summary = BuildSummary(repoPath, technologySignals, findings, roslynSummary, diSummary);

        string? llmSummary = null;
        if (request.UseLlm)
        {
            llmSummary = await TryGenerateLlmSummaryAsync(
                repoPath, technologySignals, architectureSignals, findings, nextSteps, findings, cancellationToken);
        }

        var report = new LegacyAuditReport(
            Guid.NewGuid().ToString("N"),
            repoPath,
            DateTimeOffset.UtcNow,
            summary,
            workspaceSummary,
            technologySignals,
            architectureSignals,
            findings,
            roslynSummary,
            diSummary,
            gpuSearchSummary,
            nextSteps,
            llmSummary,
            string.Empty);

        return report with { Markdown = markdownFormatter.Format(report) };
    }

    private static AuditWorkspaceSummary BuildWorkspaceSummary(DotNetWorkspaceDiscoveryResult discovery)
    {
        var slnx = discovery.Candidates.Count(c => c.Kind == DotNetWorkspaceKind.Slnx);
        var sln = discovery.Candidates.Count(c => c.Kind == DotNetWorkspaceKind.Sln);
        var csproj = discovery.Candidates.Count(c => c.Kind == DotNetWorkspaceKind.Csproj);

        return new AuditWorkspaceSummary(
            discovery.Selected?.Path,
            discovery.Selected?.Kind,
            discovery.Candidates.Count,
            slnx,
            sln,
            csproj,
            discovery.Warnings);
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

        var foundFiles = EnumerateFiles(repoPath);

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

        var appStartDirs = FindDirectories(repoPath, "App_Start");
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

        var hasTests = HasTestProjects(repoPath);
        if (hasTests)
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

    private async Task<AuditRoslynSummary> TryBuildRoslynSummaryAsync(
        string repoPath,
        DotNetWorkspaceDiscoveryResult discovery,
        List<AuditFinding> findings,
        CancellationToken cancellationToken)
    {
        try
        {
            var loadResult = await workspaceLoader.LoadAsync(discovery, cancellationToken);

            if (!loadResult.Success)
            {
                findings.Add(new AuditFinding(
                    "Info",
                    "roslyn-unavailable",
                    "Roslyn workspace could not be loaded",
                    loadResult.ErrorMessage ?? "Workspace load failed. File-based signals were still collected.",
                    Evidence: loadResult.ErrorMessage));

                return new AuditRoslynSummary(
                    false,
                    loadResult.WorkspacePath,
                    loadResult.WorkspaceKind,
                    0, 0, 0, 0, 0, 0,
                    loadResult.Warnings,
                    loadResult.ErrorMessage);
            }

            var scanResponse = await symbolScanner.ScanAsync(repoPath, cancellationToken);
            var classCount = scanResponse.Symbols.Count(s => s.Kind == "class");
            var interfaceCount = scanResponse.Symbols.Count(s => s.Kind == "interface");
            var methodCount = scanResponse.Symbols.Count(s => s.Kind == "method");

            return new AuditRoslynSummary(
                true,
                loadResult.WorkspacePath,
                loadResult.WorkspaceKind,
                loadResult.ProjectCount,
                loadResult.DocumentCount,
                scanResponse.Symbols.Count,
                classCount,
                interfaceCount,
                methodCount,
                loadResult.Warnings,
                null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            findings.Add(new AuditFinding(
                "Info",
                "roslyn-unavailable",
                "Roslyn analysis failed",
                ex.Message,
                Evidence: ex.Message));

            return new AuditRoslynSummary(
                false, null, null, 0, 0, 0, 0, 0, 0, [], ex.Message);
        }
    }

    private async Task<AuditDependencyInjectionSummary> TryBuildDiSummaryAsync(
        string repoPath,
        List<AuditFinding> findings,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await diAnalyzer.AnalyzeAsync(repoPath, cancellationToken);

            var byLifetime = result.Registrations
                .GroupBy(r => r.Lifetime, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            foreach (var finding in result.Findings)
            {
                findings.Add(new AuditFinding(
                    finding.Severity,
                    finding.Code,
                    finding.Code,
                    finding.Message,
                    finding.FilePath,
                    finding.Line));
            }

            return new AuditDependencyInjectionSummary(
                result.Registrations.Count,
                result.ConstructorDependencies.Count,
                result.Findings.Count,
                byLifetime,
                result.Findings);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            findings.Add(new AuditFinding(
                "Info",
                "di-analysis-failed",
                "DI analysis failed",
                ex.Message));

            return new AuditDependencyInjectionSummary(0, 0, 0, new Dictionary<string, int>(), []);
        }
    }

    private static void CollectDiSignals(
        AuditDependencyInjectionSummary di,
        List<TechnologySignal> signals,
        List<AuditFinding> findings)
    {
        if (di.RegistrationCount > 0)
        {
            signals.Add(new TechnologySignal(
                "Dependency injection usage",
                "Architecture",
                $"{di.RegistrationCount} DI registration(s) detected.",
                null,
                "high"));
        }
    }

    private async Task<AuditGpuSearchSummary> TryBuildGpuSearchSummaryAsync(
        string repoPath,
        bool includePresets,
        List<TechnologySignal> signals,
        List<AuditFinding> findings,
        CancellationToken cancellationToken)
    {
        try
        {
            await gpuSearchClient.GetHealthAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            findings.Add(new AuditFinding(
                "Info",
                "gpu-search-unavailable",
                "gpu-search-mcp unavailable",
                "gpu-search-mcp was not reachable. Pattern-based audit signals were skipped."));

            return new AuditGpuSearchSummary(false, 0, 0, [], ex.Message);
        }

        try
        {
            var scanRequest = new SignalScanRequest(repoPath, TopKPerSignal: MaxGpuSearchResultsPerQuery, IncludeSnippets: true);
            var scanResponse = await gpuSearchClient.ScanSignalsAsync(scanRequest, cancellationToken);
            return MapSignalScanResponse(scanResponse, signals, findings);
        }
        catch (HttpRequestException ex) when (
            ex.StatusCode == HttpStatusCode.NotFound ||
            ex.StatusCode == HttpStatusCode.MethodNotAllowed)
        {
            findings.Add(new AuditFinding(
                "Info",
                "gpu-search-scan-fallback",
                "gpu-search /scan/signals not available",
                "gpu-search-mcp /scan/signals is unavailable; fell back to individual search queries."));
        }

        return await RunIndividualQueriesAsync(repoPath, signals, findings, cancellationToken);
    }

    private static AuditGpuSearchSummary MapSignalScanResponse(
        SignalScanResponse scan,
        List<TechnologySignal> signals,
        List<AuditFinding> findings)
    {
        var results = new List<AuditGpuSearchResult>();

        foreach (var signal in scan.Signals)
        {
            foreach (var match in signal.Matches.Take(MaxGpuSearchResultsPerQuery))
            {
                if (results.Count >= MaxTotalGpuSearchResults)
                    break;

                results.Add(new AuditGpuSearchResult(
                    signal.Label,
                    match.File,
                    match.LineStart,
                    Truncate(match.Snippet, 200)));
            }

            if (signal.Matches.Count > 0)
                CollectSignalScanSignals(signal, signals, findings);
        }

        return new AuditGpuSearchSummary(
            true,
            scan.Signals.Count,
            scan.Summary?.MatchCount ?? results.Count,
            results,
            null,
            UsedSignalScan: true,
            SignalCategories: scan.Categories,
            ScanLimitations: scan.Limitations,
            ScanWarnings: scan.Warnings);
    }

    private static void CollectSignalScanSignals(
        RepositorySignal signal,
        List<TechnologySignal> signals,
        List<AuditFinding> findings)
    {
        var count = signal.Matches.Count;
        var label = signal.Label;

        switch (signal.Category)
        {
            case "framework" or "Framework":
                if (label.Contains("System.Web", StringComparison.OrdinalIgnoreCase) &&
                    !signals.Any(s => s.Name == ".NET Framework / System.Web"))
                {
                    signals.Add(new TechnologySignal(
                        ".NET Framework / System.Web",
                        "Framework",
                        $"'{label}' references found via gpu-search signal scan ({count} match(es)).",
                        null,
                        signal.Confidence ?? "medium"));
                    findings.Add(new AuditFinding(
                        "Warning",
                        "legacy-framework-detected",
                        "Legacy .NET Framework usage detected",
                        $"References to '{label}' were found. This indicates a legacy .NET Framework dependency.",
                        Evidence: $"signal scan: {count} match(es) for '{label}'"));
                }
                break;

            case "data" or "Data":
                if (!signals.Any(s => s.Name == "Direct SQL / raw query usage"))
                {
                    signals.Add(new TechnologySignal(
                        "Direct SQL / raw query usage",
                        "Data",
                        $"'{label}' usage found via gpu-search signal scan ({count} match(es)).",
                        null,
                        signal.Confidence ?? "medium"));
                    findings.Add(new AuditFinding(
                        "Warning",
                        "raw-sql-usage",
                        "Direct SQL or raw query usage detected",
                        $"'{label}' was found. Raw SQL can be a risk for injection and migration issues.",
                        Evidence: $"signal scan: {count} match(es)"));
                }
                break;

            case "quality" or "Quality":
                if (label.Contains("Exception", StringComparison.OrdinalIgnoreCase) &&
                    !signals.Any(s => s.Name == "Broad exception handling"))
                {
                    signals.Add(new TechnologySignal(
                        "Broad exception handling",
                        "Quality",
                        $"catch(Exception) found via gpu-search signal scan ({count} match(es)).",
                        null,
                        signal.Confidence ?? "medium"));
                    findings.Add(new AuditFinding(
                        "Warning",
                        "broad-exception-catch",
                        "Broad exception catch detected",
                        "catch(Exception) was found. Broad catches can hide operational failures.",
                        Evidence: $"signal scan: {count} match(es)"));
                }
                else if ((label.Contains(".Result", StringComparison.OrdinalIgnoreCase) ||
                          label.Contains(".Wait", StringComparison.OrdinalIgnoreCase)) &&
                         !signals.Any(s => s.Name == "Sync-over-async pattern"))
                {
                    signals.Add(new TechnologySignal(
                        "Sync-over-async pattern",
                        "Quality",
                        $"'{label}' found via gpu-search signal scan ({count} match(es)).",
                        null,
                        signal.Confidence ?? "medium"));
                    findings.Add(new AuditFinding(
                        "Warning",
                        "sync-over-async",
                        "Sync-over-async pattern detected",
                        $"'{label}' was found. Blocking async calls risk thread-pool starvation.",
                        Evidence: $"signal scan: {count} match(es)"));
                }
                break;

            case "architecture" or "Architecture":
                if ((label.Contains("GetService", StringComparison.OrdinalIgnoreCase) ||
                     label.Contains("GetRequiredService", StringComparison.OrdinalIgnoreCase)) &&
                    !signals.Any(s => s.Name == "Service locator pattern"))
                {
                    signals.Add(new TechnologySignal(
                        "Service locator pattern",
                        "Architecture",
                        $"'{label}' found via gpu-search signal scan ({count} match(es)).",
                        null,
                        signal.Confidence ?? "medium"));
                    findings.Add(new AuditFinding(
                        "Warning",
                        "service-locator-usage",
                        "Service locator usage detected",
                        $"'{label}' was found outside DI composition root. Service locator is an anti-pattern in DI-first apps.",
                        Evidence: $"signal scan: {count} match(es)"));
                }
                break;
        }
    }

    private async Task<AuditGpuSearchSummary> RunIndividualQueriesAsync(
        string repoPath,
        List<TechnologySignal> signals,
        List<AuditFinding> findings,
        CancellationToken cancellationToken)
    {
        var allResults = new List<AuditGpuSearchResult>();
        var queriesRun = 0;

        foreach (var query in GpuSearchAuditQueries)
        {
            if (allResults.Count >= MaxTotalGpuSearchResults)
                break;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var remaining = MaxTotalGpuSearchResults - allResults.Count;
                var limit = Math.Min(MaxGpuSearchResultsPerQuery, remaining);
                var searchResults = await gpuSearchClient.SearchHybridAsync(
                    new SearchHybridRequest(query, repoPath, limit),
                    cancellationToken);

                queriesRun++;

                foreach (var r in searchResults)
                {
                    allResults.Add(new AuditGpuSearchResult(
                        query,
                        r.File,
                        r.LineStart,
                        Truncate(r.Snippet, 200)));
                }

                if (searchResults.Count > 0)
                    CollectGpuSearchSignals(query, searchResults.Count, signals, findings);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                // skip failed individual queries
            }
        }

        return new AuditGpuSearchSummary(true, queriesRun, allResults.Count, allResults, null);
    }

    private static void CollectGpuSearchSignals(
        string query,
        int count,
        List<TechnologySignal> signals,
        List<AuditFinding> findings)
    {
        switch (query)
        {
            case "System.Web" or "System.Web.Mvc":
                if (!signals.Any(s => s.Name == ".NET Framework / System.Web"))
                {
                    signals.Add(new TechnologySignal(
                        ".NET Framework / System.Web",
                        "Framework",
                        $"'{query}' references found via gpu-search ({count} result(s)).",
                        null,
                        "medium"));
                    findings.Add(new AuditFinding(
                        "Warning",
                        "legacy-framework-detected",
                        "Legacy .NET Framework usage detected",
                        $"References to '{query}' were found. This indicates a legacy .NET Framework dependency.",
                        Evidence: $"gpu-search: {count} result(s) for '{query}'"));
                }
                break;

            case "SqlConnection" or "ExecuteSql" or "FromSql":
                if (!signals.Any(s => s.Name == "Direct SQL / raw query usage"))
                {
                    signals.Add(new TechnologySignal(
                        "Direct SQL / raw query usage",
                        "Data",
                        $"'{query}' usage found via gpu-search ({count} result(s)).",
                        null,
                        "medium"));
                    findings.Add(new AuditFinding(
                        "Warning",
                        "raw-sql-usage",
                        "Direct SQL or raw query usage detected",
                        $"'{query}' was found. Raw SQL can be a risk for injection and migration issues.",
                        Evidence: $"gpu-search: {count} result(s)"));
                }
                break;

            case "catch (Exception)":
                if (!signals.Any(s => s.Name == "Broad exception handling"))
                {
                    signals.Add(new TechnologySignal(
                        "Broad exception handling",
                        "Quality",
                        $"catch(Exception) found via gpu-search ({count} result(s)).",
                        null,
                        "medium"));
                    findings.Add(new AuditFinding(
                        "Warning",
                        "broad-exception-catch",
                        "Broad exception catch detected",
                        "catch(Exception) was found. Broad catches can hide operational failures.",
                        Evidence: $"gpu-search: {count} result(s)"));
                }
                break;

            case ".Result" or ".Wait()":
                if (!signals.Any(s => s.Name == "Sync-over-async pattern"))
                {
                    signals.Add(new TechnologySignal(
                        "Sync-over-async pattern",
                        "Quality",
                        $"'{query}' found via gpu-search ({count} result(s)).",
                        null,
                        "medium"));
                    findings.Add(new AuditFinding(
                        "Warning",
                        "sync-over-async",
                        "Sync-over-async pattern detected",
                        $"'{query}' was found. Blocking async calls risk thread-pool starvation.",
                        Evidence: $"gpu-search: {count} result(s)"));
                }
                break;

            case "GetService" or "GetRequiredService":
                if (!signals.Any(s => s.Name == "Service locator pattern"))
                {
                    signals.Add(new TechnologySignal(
                        "Service locator pattern",
                        "Architecture",
                        $"'{query}' found via gpu-search ({count} result(s)).",
                        null,
                        "medium"));
                    findings.Add(new AuditFinding(
                        "Warning",
                        "service-locator-usage",
                        "Service locator usage detected",
                        $"'{query}' was found outside DI composition root. Service locator is an anti-pattern in DI-first apps.",
                        Evidence: $"gpu-search: {count} result(s)"));
                }
                break;
        }
    }

    private static void CollectArchitectureSignals(
        IReadOnlyList<TechnologySignal> techSignals,
        AuditRoslynSummary? roslyn,
        AuditDependencyInjectionSummary? di,
        List<ArchitectureSignal> architectureSignals)
    {
        var hasLegacyFramework = techSignals.Any(s =>
            s.Category == "Framework" &&
            (s.Name.Contains(".NET Framework") || s.Name.Contains("System.Web") || s.Name.Contains("Global.asax") || s.Name.Contains("App_Start")));

        if (hasLegacyFramework)
        {
            architectureSignals.Add(new ArchitectureSignal(
                "Legacy .NET Framework application",
                "Multiple signals indicate this is a legacy .NET Framework application rather than a .NET Core/.NET 5+ application.",
                string.Join("; ", techSignals.Where(s => s.Category == "Framework").Select(s => s.Name)),
                "medium"));
        }

        if (roslyn is { WorkspaceLoaded: true })
        {
            if (roslyn.InterfaceCount > 0 && roslyn.ClassCount > 0)
            {
                var ratio = (double)roslyn.InterfaceCount / roslyn.ClassCount;
                if (ratio >= 0.15)
                {
                    architectureSignals.Add(new ArchitectureSignal(
                        "Interface-driven design",
                        "The interface-to-class ratio suggests interface-driven architecture patterns.",
                        $"{roslyn.InterfaceCount} interfaces vs {roslyn.ClassCount} classes (ratio: {ratio:F2})",
                        "medium"));
                }
            }

            if (roslyn.ProjectCount > 5)
            {
                architectureSignals.Add(new ArchitectureSignal(
                    "Multi-project solution",
                    $"Solution contains {roslyn.ProjectCount} projects. Review project dependencies and layering.",
                    $"{roslyn.ProjectCount} C# projects loaded by Roslyn",
                    "high"));
            }
        }

        if (di is { RegistrationCount: > 0 })
        {
            architectureSignals.Add(new ArchitectureSignal(
                "Dependency injection container in use",
                $"{di.RegistrationCount} DI registration(s) detected.",
                "DI registrations found by static analysis",
                "high"));
        }
    }

    private static IReadOnlyList<string> BuildRecommendedNextSteps(
        IReadOnlyList<TechnologySignal> signals,
        IReadOnlyList<AuditFinding> findings,
        AuditRoslynSummary? roslyn,
        AuditDependencyInjectionSummary? di)
    {
        var steps = new List<string>();
        var findingCodes = new HashSet<string>(findings.Select(f => f.Code), StringComparer.OrdinalIgnoreCase);

        if (findingCodes.Contains("legacy-framework-detected") || findingCodes.Contains("web-config-present"))
        {
            steps.Add("Plan migration from .NET Framework to .NET 8/9. Use the .NET Upgrade Assistant for initial guidance.");
        }

        if (findingCodes.Contains("packages-config-present"))
        {
            steps.Add("Migrate from packages.config to PackageReference in .csproj to unlock transitive dependency pruning.");
        }

        if (findingCodes.Contains("no-tests-detected"))
        {
            steps.Add("Add unit and integration tests before starting modernization. Tests are a safety net for refactoring.");
        }

        if (findingCodes.Contains("broad-exception-catch"))
        {
            steps.Add("Replace broad catch(Exception) handlers with specific exception types or structured error handling.");
        }

        if (findingCodes.Contains("sync-over-async"))
        {
            steps.Add("Replace .Result and .Wait() blocking calls with async/await throughout the call chain.");
        }

        if (findingCodes.Contains("raw-sql-usage"))
        {
            steps.Add("Review direct SQL usage for injection risks. Consider migrating to parameterized queries or a safe ORM layer.");
        }

        if (findingCodes.Contains("service-locator-usage"))
        {
            steps.Add("Replace service locator calls with constructor injection. Service locator hides dependencies and complicates testing.");
        }

        if (di is { FindingCount: > 0 })
        {
            steps.Add("Review DI analysis findings. Address singleton-depends-on-scoped and missing registration candidates first.");
        }

        if (roslyn is { WorkspaceLoaded: false })
        {
            steps.Add("Resolve Roslyn workspace load failures to unlock deeper compiler-aware analysis.");
        }

        if (steps.Count == 0)
        {
            steps.Add("Review technology signals and architecture signals above for specific action areas.");
            steps.Add("Run with IncludeRoslyn=true and IncludeGpuSearch=true for the most complete audit.");
        }

        return steps;
    }

    private static string BuildSummary(
        string repoPath,
        IReadOnlyList<TechnologySignal> signals,
        IReadOnlyList<AuditFinding> findings,
        AuditRoslynSummary? roslyn,
        AuditDependencyInjectionSummary? di)
    {
        var highCount = findings.Count(f => f.Severity == "High");
        var warnCount = findings.Count(f => f.Severity == "Warning");
        var infoCount = findings.Count(f => f.Severity == "Info");

        var parts = new List<string>
        {
            $"Audit of `{repoPath}` completed.",
            $"{signals.Count} technology signal(s) detected.",
            $"{findings.Count} finding(s): {highCount} High, {warnCount} Warning, {infoCount} Info."
        };

        if (roslyn is { WorkspaceLoaded: true })
        {
            parts.Add($"Roslyn loaded {roslyn.ProjectCount} project(s), {roslyn.DocumentCount} document(s), {roslyn.SymbolCount} symbol(s).");
        }

        if (di is { RegistrationCount: > 0 })
        {
            parts.Add($"DI analysis found {di.RegistrationCount} registration(s).");
        }

        return string.Join(" ", parts);
    }

    private async Task<string?> TryGenerateLlmSummaryAsync(
        string repoPath,
        IReadOnlyList<TechnologySignal> signals,
        IReadOnlyList<ArchitectureSignal> architectureSignals,
        IReadOnlyList<AuditFinding> findings,
        IReadOnlyList<string> nextSteps,
        IReadOnlyList<AuditFinding> allFindings,
        CancellationToken cancellationToken)
    {
        try
        {
            var prompt = BuildAuditLlmPrompt(repoPath, signals, architectureSignals, findings, nextSteps);
            var fakeReviewFindings = allFindings
                .Take(20)
                .Select(f => new ReviewFinding(f.Code, f.Severity, f.FilePath, f.Title, f.Message))
                .ToList();

            return await llmProvider.GenerateReviewAsync(
                new LlmReviewPrompt(repoPath, fakeReviewFindings, prompt),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private static string BuildAuditLlmPrompt(
        string repoPath,
        IReadOnlyList<TechnologySignal> signals,
        IReadOnlyList<ArchitectureSignal> architectureSignals,
        IReadOnlyList<AuditFinding> findings,
        IReadOnlyList<string> nextSteps)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are reviewing a Legacy .NET audit report for repository: {repoPath}");
        sb.AppendLine();
        sb.AppendLine("Technology signals detected:");

        foreach (var signal in signals.Take(15))
            sb.AppendLine($"- {signal.Name} [{signal.Category}] (confidence: {signal.Confidence}): {signal.Evidence}");

        sb.AppendLine();
        sb.AppendLine("Architecture signals:");

        foreach (var signal in architectureSignals.Take(5))
            sb.AppendLine($"- {signal.Name}: {signal.Message}");

        sb.AppendLine();
        sb.AppendLine("Risk findings:");

        foreach (var finding in findings.Take(15))
            sb.AppendLine($"- [{finding.Severity}] {finding.Code}: {finding.Message}");

        sb.AppendLine();
        sb.AppendLine("Recommended next steps:");

        foreach (var step in nextSteps)
            sb.AppendLine($"- {step}");

        sb.AppendLine();
        sb.AppendLine("Provide a concise executive summary (3-5 sentences) of the legacy .NET audit findings and modernization priorities.");
        sb.AppendLine("Do not invent findings. Do not claim tests were run. Base your summary only on the data above.");

        return sb.ToString();
    }

    private static IReadOnlyList<string> EnumerateFiles(string root)
    {
        try
        {
            return Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    var parts = f.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
                    return !parts.Any(p =>
                        string.Equals(p, "bin", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p, "obj", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p, ".git", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p, "node_modules", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p, "packages", StringComparison.OrdinalIgnoreCase));
                })
                .ToList();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> FindDirectories(string root, string name)
    {
        try
        {
            return Directory.GetDirectories(root, name, SearchOption.AllDirectories).ToList();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return [];
        }
    }

    private static bool HasTestProjects(string root)
    {
        try
        {
            return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Any(f =>
                    f.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith("Spec.cs", StringComparison.OrdinalIgnoreCase))
                || Directory.GetDirectories(root, "*", SearchOption.AllDirectories)
                    .Any(d =>
                    {
                        var name = Path.GetFileName(d);
                        return name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) ||
                               name.EndsWith(".Test", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Tests", StringComparison.OrdinalIgnoreCase);
                    });
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return string.Concat(value.AsSpan(0, maxLength), "...");
    }
}
