namespace LegacyLens.Application.DotNetAnalysis;

public interface IRoslynDependencyInjectionAnalyzer
{
    Task<DependencyInjectionAnalysisResult> AnalyzeAsync(
        string repoPath,
        CancellationToken cancellationToken);
}
