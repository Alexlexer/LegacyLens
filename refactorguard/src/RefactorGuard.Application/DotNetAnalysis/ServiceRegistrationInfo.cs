namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record ServiceRegistrationInfo(
    string? ServiceType,
    string? ImplementationType,
    string Lifetime,
    string FilePath,
    int Line,
    int Column,
    string ProjectName,
    string RegistrationExpression);
