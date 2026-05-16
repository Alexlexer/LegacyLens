using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Application.Audit;

public sealed record AuditRoslynSummary(
    bool WorkspaceLoaded,
    string? WorkspacePath,
    DotNetWorkspaceKind? WorkspaceKind,
    int ProjectCount,
    int DocumentCount,
    int SymbolCount,
    int ClassCount,
    int InterfaceCount,
    int MethodCount,
    IReadOnlyList<string> Warnings,
    string? ErrorMessage);
