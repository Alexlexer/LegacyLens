using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Application.Review;

public sealed record RoslynReviewContext(
    bool Success,
    string? WorkspacePath,
    DotNetWorkspaceKind? WorkspaceKind,
    IReadOnlyList<ChangedSymbolSummary> ChangedSymbols,
    IReadOnlyList<RoslynReferenceSummary> SymbolReferences,
    IReadOnlyList<string> Warnings,
    string? ErrorMessage);

public sealed record ChangedSymbolSummary(
    string Name,
    string FullName,
    string Kind,
    string FilePath,
    int Line,
    int Column,
    string ProjectName);

public sealed record RoslynReferenceSummary(
    string SymbolName,
    string SymbolFullName,
    string SymbolKind,
    string FilePath,
    int Line,
    int Column,
    string ProjectName,
    string? ContainingSymbol,
    string ReferenceKind,
    bool IsDefinition);
