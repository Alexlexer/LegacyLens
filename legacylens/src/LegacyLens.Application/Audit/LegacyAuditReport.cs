namespace LegacyLens.Application.Audit;

public sealed record LegacyAuditReport(
    string ReportId,
    string RepoPath,
    DateTimeOffset GeneratedAtUtc,
    string Summary,
    AuditWorkspaceSummary WorkspaceSummary,
    IReadOnlyList<TechnologySignal> TechnologySignals,
    IReadOnlyList<ArchitectureSignal> ArchitectureSignals,
    IReadOnlyList<AuditFinding> RiskFindings,
    AuditRoslynSummary? RoslynSummary,
    AuditDependencyInjectionSummary? DependencyInjectionSummary,
    AuditGpuSearchSummary? GpuSearchSummary,
    IReadOnlyList<string> RecommendedNextSteps,
    string? LlmSummary,
    string Markdown);
