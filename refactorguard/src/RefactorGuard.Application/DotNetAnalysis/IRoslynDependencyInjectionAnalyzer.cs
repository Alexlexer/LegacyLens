namespace RefactorGuard.Application.DotNetAnalysis;

public interface IRoslynDependencyInjectionAnalyzer
{
    Task<DependencyInjectionAnalysisResult> AnalyzeAsync(
        string repoPath,
        CancellationToken cancellationToken);
}
