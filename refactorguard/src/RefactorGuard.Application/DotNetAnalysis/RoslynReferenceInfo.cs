namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record RoslynReferenceInfo(
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
