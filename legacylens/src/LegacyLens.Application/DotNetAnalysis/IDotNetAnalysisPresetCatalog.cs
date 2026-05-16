namespace LegacyLens.Application.DotNetAnalysis;

public interface IDotNetAnalysisPresetCatalog
{
    IReadOnlyList<DotNetAnalysisPreset> GetAll();

    IReadOnlyList<DotNetAnalysisPreset> Resolve(IReadOnlyList<string>? presetIds);
}
