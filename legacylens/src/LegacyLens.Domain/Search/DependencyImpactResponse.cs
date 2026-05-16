namespace LegacyLens.Domain.Search;

public sealed record DependencyImpactResponse(
    string Result,
    string File,
    string? AbsoluteFile,
    IReadOnlyList<ImpactedFile> ImpactedFiles,
    string? Confidence = null,
    string? AnalysisMode = null,
    IReadOnlyList<string>? Limitations = null,
    IReadOnlyList<string>? Warnings = null);
