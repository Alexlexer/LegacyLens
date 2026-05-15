namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record DotNetAnalysisFinding(
    string PresetId,
    string Severity,
    string FilePath,
    int? Line,
    string Snippet,
    string Rationale);
