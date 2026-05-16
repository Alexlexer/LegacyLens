using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Application.Audit;

public sealed record AuditContext(
    string RepoPath,
    LegacyAuditRequest Request,
    DotNetWorkspaceDiscoveryResult WorkspaceDiscovery,
    AuditWorkspaceSummary WorkspaceSummary,
    IReadOnlyList<TechnologySignal> TechnologySignals,
    IReadOnlyList<ArchitectureSignal> ArchitectureSignals,
    IReadOnlyList<AuditFinding> RiskFindings,
    AuditRoslynSummary? RoslynSummary,
    AuditDependencyInjectionSummary? DependencyInjectionSummary,
    AuditGpuSearchSummary? GpuSearchSummary);
