namespace LegacyLens.Application.DotNetAnalysis;

public interface IRoslynReferenceAnalyzer
{
    Task<RoslynReferenceAnalysisResult> FindReferencesAsync(
        RoslynReferenceAnalysisRequest request,
        CancellationToken cancellationToken);
}
