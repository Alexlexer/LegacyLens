namespace LegacyLens.Application.DotNetAnalysis;

public sealed record DotNetSymbolInfo(
    string Name,
    string FullName,
    string Kind,
    string FilePath,
    int Line,
    int Column,
    string ProjectName);
