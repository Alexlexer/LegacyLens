namespace LegacyLens.Application.Audit;

public sealed record AuditProviderResult(
    string ProviderName,
    IReadOnlyList<TechnologySignal>? TechnologySignals = null,
    IReadOnlyList<ArchitectureSignal>? ArchitectureSignals = null,
    IReadOnlyList<AuditFinding>? RiskFindings = null,
    IReadOnlyList<string>? RecommendedNextSteps = null,
    IReadOnlyList<string>? Warnings = null,
    AuditRoslynSummary? RoslynSummary = null,
    AuditDependencyInjectionSummary? DependencyInjectionSummary = null,
    AuditGpuSearchSummary? GpuSearchSummary = null);
