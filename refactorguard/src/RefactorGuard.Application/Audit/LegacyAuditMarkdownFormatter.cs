using System.Text;

namespace RefactorGuard.Application.Audit;

public sealed class LegacyAuditMarkdownFormatter : ILegacyAuditMarkdownFormatter
{
    public string Format(LegacyAuditReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Legacy .NET Audit Report");
        sb.AppendLine();
        sb.AppendLine($"**Report ID:** {report.ReportId}");
        sb.AppendLine($"**Repository:** {report.RepoPath}");
        sb.AppendLine($"**Generated:** {report.GeneratedAtUtc:O}");
        sb.AppendLine();

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine(report.Summary);
        sb.AppendLine();

        AppendWorkspaceSection(sb, report.WorkspaceSummary);
        AppendTechnologySignals(sb, report.TechnologySignals);
        AppendArchitectureSignals(sb, report.ArchitectureSignals);
        AppendRiskFindings(sb, report.RiskFindings);
        AppendRoslynSummary(sb, report.RoslynSummary);
        AppendDependencyInjectionSummary(sb, report.DependencyInjectionSummary);
        AppendGpuSearchSection(sb, report.GpuSearchSummary);
        AppendRecommendedNextSteps(sb, report.RecommendedNextSteps);

        if (!string.IsNullOrWhiteSpace(report.LlmSummary))
        {
            sb.AppendLine("## LLM Summary");
            sb.AppendLine();
            sb.AppendLine(report.LlmSummary);
            sb.AppendLine();
        }

        AppendLimitations(sb, report);

        return sb.ToString().TrimEnd();
    }

    private static void AppendWorkspaceSection(StringBuilder sb, AuditWorkspaceSummary ws)
    {
        sb.AppendLine("## Workspace");
        sb.AppendLine();

        if (ws.SelectedWorkspacePath is not null)
        {
            sb.AppendLine($"- **Selected:** `{ws.SelectedWorkspacePath}` ({ws.SelectedWorkspaceKind})");
        }
        else
        {
            sb.AppendLine("- **Selected:** No workspace file found.");
        }

        sb.AppendLine($"- Total candidates: {ws.TotalCandidates} ({ws.SlnxCount} .slnx, {ws.SlnCount} .sln, {ws.CsprojCount} .csproj)");

        foreach (var warning in ws.Warnings)
            sb.AppendLine($"- ⚠ {warning}");

        sb.AppendLine();
    }

    private static void AppendTechnologySignals(StringBuilder sb, IReadOnlyList<TechnologySignal> signals)
    {
        sb.AppendLine("## Technology Signals");
        sb.AppendLine();

        if (signals.Count == 0)
        {
            sb.AppendLine("No technology signals detected.");
            sb.AppendLine();
            return;
        }

        foreach (var signal in signals)
        {
            var location = signal.FilePath is not null ? $" (`{signal.FilePath}`)" : string.Empty;
            sb.AppendLine($"- **{signal.Name}** [{signal.Category}]{location}");
            sb.AppendLine($"  - {signal.Evidence} *(confidence: {signal.Confidence})*");
        }

        sb.AppendLine();
    }

    private static void AppendArchitectureSignals(StringBuilder sb, IReadOnlyList<ArchitectureSignal> signals)
    {
        sb.AppendLine("## Architecture Signals");
        sb.AppendLine();

        if (signals.Count == 0)
        {
            sb.AppendLine("No architecture signals detected.");
            sb.AppendLine();
            return;
        }

        foreach (var signal in signals)
        {
            sb.AppendLine($"- **{signal.Name}**: {signal.Message}");
            sb.AppendLine($"  - {signal.Evidence} *(confidence: {signal.Confidence})*");
        }

        sb.AppendLine();
    }

    private static void AppendRiskFindings(StringBuilder sb, IReadOnlyList<AuditFinding> findings)
    {
        sb.AppendLine("## Risk Findings");
        sb.AppendLine();

        if (findings.Count == 0)
        {
            sb.AppendLine("No risk findings detected.");
            sb.AppendLine();
            return;
        }

        foreach (var finding in findings)
        {
            var location = finding.FilePath is not null
                ? $" `{finding.FilePath}`{(finding.Line.HasValue ? $":{finding.Line}" : string.Empty)}"
                : string.Empty;
            sb.AppendLine($"- **[{finding.Severity}]** `{finding.Code}` — {finding.Title}{location}");
            sb.AppendLine($"  - {finding.Message}");

            if (!string.IsNullOrWhiteSpace(finding.Evidence))
                sb.AppendLine($"  - *Evidence: {finding.Evidence}*");
        }

        sb.AppendLine();
    }

    private static void AppendRoslynSummary(StringBuilder sb, AuditRoslynSummary? roslyn)
    {
        sb.AppendLine("## Roslyn Summary");
        sb.AppendLine();

        if (roslyn is null)
        {
            sb.AppendLine("Roslyn analysis was not requested.");
            sb.AppendLine();
            return;
        }

        if (!roslyn.WorkspaceLoaded)
        {
            sb.AppendLine("Roslyn workspace could not be loaded.");

            if (!string.IsNullOrWhiteSpace(roslyn.ErrorMessage))
                sb.AppendLine($"**Error:** {roslyn.ErrorMessage}");

            foreach (var warning in roslyn.Warnings)
                sb.AppendLine($"- ⚠ {warning}");

            sb.AppendLine();
            return;
        }

        sb.AppendLine($"- **Workspace:** `{roslyn.WorkspacePath}` ({roslyn.WorkspaceKind})");
        sb.AppendLine($"- Projects: {roslyn.ProjectCount}");
        sb.AppendLine($"- Documents: {roslyn.DocumentCount}");
        sb.AppendLine($"- Symbols: {roslyn.SymbolCount}");
        sb.AppendLine($"- Classes: {roslyn.ClassCount}, Interfaces: {roslyn.InterfaceCount}, Methods: {roslyn.MethodCount}");

        foreach (var warning in roslyn.Warnings)
            sb.AppendLine($"- ⚠ {warning}");

        sb.AppendLine();
    }

    private static void AppendDependencyInjectionSummary(StringBuilder sb, AuditDependencyInjectionSummary? di)
    {
        sb.AppendLine("## Dependency Injection Summary");
        sb.AppendLine();

        if (di is null)
        {
            sb.AppendLine("DI analysis was not requested.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"- Registrations: {di.RegistrationCount}");
        sb.AppendLine($"- Constructor dependencies: {di.ConstructorDependencyCount}");
        sb.AppendLine($"- Findings: {di.FindingCount}");

        if (di.RegistrationsByLifetime.Count > 0)
        {
            var lifetimes = string.Join(", ", di.RegistrationsByLifetime.Select(kv => $"{kv.Key}: {kv.Value}"));
            sb.AppendLine($"- By lifetime: {lifetimes}");
        }

        foreach (var finding in di.Findings)
        {
            var location = finding.FilePath is not null
                ? $" `{finding.FilePath}`{(finding.Line.HasValue ? $":{finding.Line}" : string.Empty)}"
                : string.Empty;
            sb.AppendLine($"  - **[{finding.Severity}]** `{finding.Code}` — {finding.Message}{location}");
        }

        sb.AppendLine();
    }

    private static void AppendGpuSearchSection(StringBuilder sb, AuditGpuSearchSummary? gpuSearch)
    {
        sb.AppendLine("## gpu-search Signal Scan");
        sb.AppendLine();

        if (gpuSearch is null)
        {
            sb.AppendLine("gpu-search analysis was not requested.");
            sb.AppendLine();
            return;
        }

        if (!gpuSearch.WasAvailable)
        {
            sb.AppendLine("gpu-search was unavailable or returned errors.");

            if (!string.IsNullOrWhiteSpace(gpuSearch.ErrorMessage))
                sb.AppendLine($"**Error:** {gpuSearch.ErrorMessage}");

            sb.AppendLine();
            return;
        }

        if (gpuSearch.UsedSignalScan)
        {
            sb.AppendLine("**Mode:** Signal scan (`POST /scan/signals`)");

            if (gpuSearch.SignalCategories is { Count: > 0 })
                sb.AppendLine($"- Categories: {string.Join(", ", gpuSearch.SignalCategories)}");

            sb.AppendLine($"- Signals scanned: {gpuSearch.QueriesRun}");
            sb.AppendLine($"- Total matches: {gpuSearch.TotalResults}");

            if (gpuSearch.ScanWarnings is { Count: > 0 })
            {
                foreach (var warning in gpuSearch.ScanWarnings)
                    sb.AppendLine($"- ⚠ {warning}");
            }

            if (gpuSearch.ScanLimitations is { Count: > 0 })
            {
                sb.AppendLine();
                sb.AppendLine("**Scan limitations:**");
                foreach (var limitation in gpuSearch.ScanLimitations)
                    sb.AppendLine($"- {limitation}");
            }
        }
        else
        {
            sb.AppendLine("**Mode:** Individual queries (fallback)");
            sb.AppendLine($"- Queries run: {gpuSearch.QueriesRun}");
            sb.AppendLine($"- Total results: {gpuSearch.TotalResults}");
        }

        sb.AppendLine();

        if (gpuSearch.Results.Count > 0)
        {
            sb.AppendLine("*Note: gpu-search results are heuristic/retrieval-based, not compiler-verified.*");
            sb.AppendLine();

            foreach (var result in gpuSearch.Results)
            {
                var location = result.FilePath is not null
                    ? $" `{result.FilePath}`{(result.Line.HasValue ? $":{result.Line}" : string.Empty)}"
                    : string.Empty;
                sb.AppendLine($"- `{result.Query}`{location}");

                if (!string.IsNullOrWhiteSpace(result.Snippet))
                    sb.AppendLine($"  ```\n  {result.Snippet.Replace("\n", "\n  ").TrimEnd()}\n  ```");
            }
        }

        sb.AppendLine();
    }

    private static void AppendRecommendedNextSteps(StringBuilder sb, IReadOnlyList<string> steps)
    {
        sb.AppendLine("## Recommended Next Steps");
        sb.AppendLine();

        if (steps.Count == 0)
        {
            sb.AppendLine("No recommendations generated.");
            sb.AppendLine();
            return;
        }

        foreach (var step in steps)
            sb.AppendLine($"- {step}");

        sb.AppendLine();
    }

    private static void AppendLimitations(StringBuilder sb, LegacyAuditReport report)
    {
        sb.AppendLine("## Limitations");
        sb.AppendLine();
        sb.AppendLine("- This report is based on **static analysis only**. No code was executed or modified.");
        sb.AppendLine("- **Roslyn facts** are compiler-aware only when the workspace loads successfully. Workspace load failures fall back to file-based signals.");
        sb.AppendLine("- **gpu-search findings** are heuristic and retrieval-based. They are not compiler-verified.");
        sb.AppendLine("- **DI analysis findings** are static and advisory. They are not runtime container verification.");

        if (!string.IsNullOrWhiteSpace(report.LlmSummary))
            sb.AppendLine("- **LLM summary** is optional and advisory. It may contain inaccuracies. Do not treat it as authoritative.");

        sb.AppendLine();
    }
}
