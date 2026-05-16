namespace LegacyLens.Application.DotNetAnalysis;

public sealed record DotNetWorkspaceCandidate(
    string Path,
    DotNetWorkspaceKind Kind,
    bool IsSelected,
    IReadOnlyList<string> Warnings);
