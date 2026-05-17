using System.Net;
using LegacyLens.Application.Search;
using LegacyLens.Domain.Search;

namespace LegacyLens.Application.Audit;

public sealed class GpuSearchSignalAuditProvider(
    IGpuSearchClient gpuSearchClient) : IAuditProvider
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

    public async Task<AuditProviderResult> AnalyzeAsync(
        AuditContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.IncludeGpuSearch)
            return new AuditProviderResult(Name);

        var signals = new List<TechnologySignal>();
        var findings = new List<AuditFinding>();
        var summary = await TryBuildGpuSearchSummaryAsync(
            context.RepoPath,
            signals,
            findings,
            cancellationToken);

        return new AuditProviderResult(
            Name,
            TechnologySignals: signals,
            RiskFindings: findings,
            GpuSearchSummary: summary);
    }

    public string Name => "GpuSearchSignal";

    private async Task<AuditGpuSearchSummary> TryBuildGpuSearchSummaryAsync(
        string repoPath,
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

        await TryIndexRepoAsync(repoPath, findings, cancellationToken);

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

    private async Task TryIndexRepoAsync(
        string repoPath,
        List<AuditFinding> findings,
        CancellationToken cancellationToken)
    {
        try
        {
            var indexResponse = await gpuSearchClient.IndexRootAsync(
                new GpuSearchIndexRootRequest(repoPath),
                cancellationToken);

            if (!indexResponse.Ok)
            {
                findings.Add(new AuditFinding(
                    "Info",
                    "gpu-search-index-warning",
                    "gpu-search repo index incomplete",
                    indexResponse.Message ?? "gpu-search-mcp could not fully index the repository before signal scanning."));
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            findings.Add(new AuditFinding(
                "Info",
                "gpu-search-index-warning",
                "gpu-search repo index skipped",
                $"gpu-search-mcp /index/root was not available; signal scan may cover a different repository. ({ex.Message})"));
        }
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
                AddOnce(".NET Framework / System.Web", "Framework", $"'{query}' references found via gpu-search ({count} result(s)).", "legacy-framework-detected", "Legacy .NET Framework usage detected", $"References to '{query}' were found. This indicates a legacy .NET Framework dependency.", $"gpu-search: {count} result(s) for '{query}'");
                break;
            case "SqlConnection" or "ExecuteSql" or "FromSql":
                AddOnce("Direct SQL / raw query usage", "Data", $"'{query}' usage found via gpu-search ({count} result(s)).", "raw-sql-usage", "Direct SQL or raw query usage detected", $"'{query}' was found. Raw SQL can be a risk for injection and migration issues.", $"gpu-search: {count} result(s)");
                break;
            case "catch (Exception)":
                AddOnce("Broad exception handling", "Quality", $"catch(Exception) found via gpu-search ({count} result(s)).", "broad-exception-catch", "Broad exception catch detected", "catch(Exception) was found. Broad catches can hide operational failures.", $"gpu-search: {count} result(s)");
                break;
            case ".Result" or ".Wait()":
                AddOnce("Sync-over-async pattern", "Quality", $"'{query}' found via gpu-search ({count} result(s)).", "sync-over-async", "Sync-over-async pattern detected", $"'{query}' was found. Blocking async calls risk thread-pool starvation.", $"gpu-search: {count} result(s)");
                break;
            case "GetService" or "GetRequiredService":
                AddOnce("Service locator pattern", "Architecture", $"'{query}' found via gpu-search ({count} result(s)).", "service-locator-usage", "Service locator usage detected", $"'{query}' was found outside DI composition root. Service locator is an anti-pattern in DI-first apps.", $"gpu-search: {count} result(s)");
                break;
        }

        void AddOnce(string signalName, string category, string evidence, string code, string title, string message, string findingEvidence)
        {
            if (signals.Any(s => s.Name == signalName))
                return;

            signals.Add(new TechnologySignal(signalName, category, evidence, null, "medium"));
            findings.Add(new AuditFinding("Warning", code, title, message, Evidence: findingEvidence));
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return string.Concat(value.AsSpan(0, maxLength), "...");
    }
}
