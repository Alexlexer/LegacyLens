namespace LegacyLens.Application.DotNetAnalysis;

public sealed record RoslynWorkspaceLoadResult(
    bool Success,
    string? WorkspacePath,
    DotNetWorkspaceKind? WorkspaceKind,
    int ProjectCount,
    int DocumentCount,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<string> Warnings,
    string? ErrorMessage);
