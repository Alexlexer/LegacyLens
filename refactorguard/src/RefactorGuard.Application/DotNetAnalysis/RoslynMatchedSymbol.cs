namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record RoslynMatchedSymbol(
    string Name,
    string FullName,
    string Kind,
    string FilePath,
    int Line,
    int Column,
    string ProjectName);
