using System.Text;
using LegacyLens.Application.DotNetAnalysis;
using LegacyLens.Application.Review;

namespace LegacyLens.Application.Audit;

public sealed class LegacyAuditOrchestrator(
    IDotNetWorkspaceDiscovery workspaceDiscovery,
    IEnumerable<IAuditProvider> auditProviders,
    IReviewLlmProvider llmProvider,
    ILegacyAuditMarkdownFormatter markdownFormatter) : ILegacyAuditOrchestrator
{
    public async Task<LegacyAuditReport> AuditAsync(
        LegacyAuditRequest request,
        CancellationToken cancellationToken)
    {
        var repoPath = request.RepoPath ?? string.Empty;
        var findings = new List<AuditFinding>();
        var technologySignals = new List<TechnologySignal>();
        var architectureSignals = new List<ArchitectureSignal>();
        var nextSteps = new List<string>();

        var discoveryResult = await workspaceDiscovery.DiscoverAsync(repoPath, cancellationToken);
        var workspaceSummary = BuildWorkspaceSummary(discoveryResult);
        AuditRoslynSummary? roslynSummary = null;
        AuditDependencyInjectionSummary? diSummary = null;
        AuditGpuSearchSummary? gpuSearchSummary = null;

        foreach (var provider in auditProviders)
        {
            var context = new AuditContext(
                repoPath,
                request,
                discoveryResult,
                workspaceSummary,
                technologySignals,
                architectureSignals,
                findings,
                roslynSummary,
                diSummary,
                gpuSearchSummary);

            AuditProviderResult result;
            try
            {
                result = await provider.AnalyzeAsync(context, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                findings.Add(new AuditFinding(
                    "Info",
                    "audit-provider-failed",
                    $"{provider.Name} audit provider failed",
                    ex.Message,
                    Evidence: provider.Name));
                continue;
            }

            Merge(result, technologySignals, architectureSignals, findings, nextSteps);
            roslynSummary = result.RoslynSummary ?? roslynSummary;
            diSummary = result.DependencyInjectionSummary ?? diSummary;
            gpuSearchSummary = result.GpuSearchSummary ?? gpuSearchSummary;
        }

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

    private static void Merge(
        AuditProviderResult result,
        List<TechnologySignal> technologySignals,
        List<ArchitectureSignal> architectureSignals,
        List<AuditFinding> findings,
        List<string> nextSteps)
    {
        foreach (var signal in result.TechnologySignals ?? [])
        {
            if (!technologySignals.Any(existing =>
                    existing.Name == signal.Name &&
                    existing.Category == signal.Category &&
                    existing.FilePath == signal.FilePath))
            {
                technologySignals.Add(signal);
            }
        }

        foreach (var signal in result.ArchitectureSignals ?? [])
        {
            if (!architectureSignals.Any(existing =>
                    existing.Name == signal.Name &&
                    existing.Message == signal.Message))
            {
                architectureSignals.Add(signal);
            }
        }

        foreach (var finding in result.RiskFindings ?? [])
        {
            if (!findings.Any(existing =>
                    existing.Code == finding.Code &&
                    existing.FilePath == finding.FilePath &&
                    existing.Line == finding.Line))
            {
                findings.Add(finding);
            }
        }

        foreach (var step in result.RecommendedNextSteps ?? [])
        {
            if (!nextSteps.Contains(step, StringComparer.OrdinalIgnoreCase))
                nextSteps.Add(step);
        }

        foreach (var warning in result.Warnings ?? [])
        {
            findings.Add(new AuditFinding(
                "Info",
                "audit-provider-warning",
                $"{result.ProviderName} audit provider warning",
                warning,
                Evidence: result.ProviderName));
        }
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
            parts.Add($"Roslyn loaded {roslyn.ProjectCount} project(s), {roslyn.DocumentCount} document(s), {roslyn.SymbolCount} symbol(s).");

        if (di is { RegistrationCount: > 0 })
            parts.Add($"DI analysis found {di.RegistrationCount} registration(s).");

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
}
