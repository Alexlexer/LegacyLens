namespace LegacyLens.Application.DotNetAnalysis;

public sealed record DotNetAnalysisResponse(
    IReadOnlyList<DotNetAnalysisPresetResult> Presets,
    IReadOnlyList<DotNetAnalysisFinding> Findings);
