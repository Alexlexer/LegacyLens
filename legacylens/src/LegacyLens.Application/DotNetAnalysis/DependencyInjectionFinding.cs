namespace LegacyLens.Application.DotNetAnalysis;

public sealed record DependencyInjectionFinding(
    string Severity,
    string Code,
    string Message,
    string? FilePath,
    int? Line,
    int? Column);
