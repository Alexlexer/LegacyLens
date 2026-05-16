using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Application.Audit;

public sealed record AuditWorkspaceSummary(
    string? SelectedWorkspacePath,
    DotNetWorkspaceKind? SelectedWorkspaceKind,
    int TotalCandidates,
    int SlnxCount,
    int SlnCount,
    int CsprojCount,
    IReadOnlyList<string> Warnings);
