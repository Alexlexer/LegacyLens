namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record DotNetAnalysisPresetResult(
    string PresetId,
    string Title,
    int MatchCount);
