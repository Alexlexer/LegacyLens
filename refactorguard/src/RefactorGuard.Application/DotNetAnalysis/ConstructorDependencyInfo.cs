namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record ConstructorDependencyInfo(
    string ContainingType,
    string DependencyType,
    string FilePath,
    int Line,
    int Column,
    string ProjectName);
