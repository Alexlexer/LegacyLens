using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Application.Audit;

public sealed class DependencyInjectionAuditProvider(
    IRoslynDependencyInjectionAnalyzer diAnalyzer) : IAuditProvider
{
    public string Name => "DependencyInjection";

    public async Task<AuditProviderResult> AnalyzeAsync(
        AuditContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.IncludeDependencyInjection)
            return new AuditProviderResult(Name);

        try
        {
            var result = await diAnalyzer.AnalyzeAsync(context.RepoPath, cancellationToken);
            var byLifetime = result.Registrations
                .GroupBy(r => r.Lifetime, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var findings = result.Findings
                .Select(finding => new AuditFinding(
                    finding.Severity,
                    finding.Code,
                    finding.Code,
                    finding.Message,
                    finding.FilePath,
                    finding.Line))
                .ToList();

            var signals = new List<TechnologySignal>();
            if (result.Registrations.Count > 0)
            {
                signals.Add(new TechnologySignal(
                    "Dependency injection usage",
                    "Architecture",
                    $"{result.Registrations.Count} DI registration(s) detected.",
                    null,
                    "high"));
            }

            return new AuditProviderResult(
                Name,
                TechnologySignals: signals,
                RiskFindings: findings,
                DependencyInjectionSummary: new AuditDependencyInjectionSummary(
                    result.Registrations.Count,
                    result.ConstructorDependencies.Count,
                    result.Findings.Count,
                    byLifetime,
                    result.Findings));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AuditProviderResult(
                Name,
                RiskFindings:
                [
                    new AuditFinding(
                        "Info",
                        "di-analysis-failed",
                        "DI analysis failed",
                        ex.Message)
                ],
                DependencyInjectionSummary: new AuditDependencyInjectionSummary(
                    0, 0, 0, new Dictionary<string, int>(), []));
        }
    }
}
