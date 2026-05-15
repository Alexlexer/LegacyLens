using RefactorGuard.Application.Search;
using RefactorGuard.Domain.Search;

namespace RefactorGuard.Application.DotNetAnalysis;

public sealed class DotNetAnalysisService(
    IDotNetAnalysisPresetCatalog presetCatalog,
    IGpuSearchClient gpuSearchClient)
{
    public async Task<DotNetAnalysisResponse> AnalyzeAsync(
        DotNetAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var presets = presetCatalog.Resolve(request.Presets);
        var limit = Math.Clamp(request.LimitPerPreset, 1, 50);
        var presetResults = new List<DotNetAnalysisPresetResult>();
        var findings = new List<DotNetAnalysisFinding>();

        foreach (var preset in presets)
        {
            var results = await gpuSearchClient.SearchHybridAsync(
                new SearchHybridRequest(preset.Query, request.RepoPath, limit),
                cancellationToken);

            presetResults.Add(new DotNetAnalysisPresetResult(preset.Id, preset.Title, results.Count));
            findings.AddRange(results.Select(result => new DotNetAnalysisFinding(
                preset.Id,
                "Review",
                result.FilePath,
                result.Line,
                result.Snippet,
                preset.Rationale)));
        }

        return new DotNetAnalysisResponse(presetResults, findings);
    }
}
