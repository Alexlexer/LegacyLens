using Microsoft.Extensions.DependencyInjection;
using RefactorGuard.Application.DotNetAnalysis;
using RefactorGuard.Application.Git;
using RefactorGuard.Application.Review;
using RefactorGuard.Application.Search;

namespace RefactorGuard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRefactorGuardApplication(this IServiceCollection services)
    {
        services.AddScoped<GitDiffPreviewWorkflow>();
        services.AddScoped<GpuSearchStatusWorkflow>();
        services.AddSingleton<IDotNetAnalysisPresetCatalog, DotNetAnalysisPresetCatalog>();
        services.AddScoped<DotNetAnalysisService>();
        services.AddScoped<IReviewOrchestrator, DiffReviewOrchestrator>();
        services.AddSingleton<IReviewReportFormatter, MarkdownReviewReportFormatter>();
        services.AddSingleton<IReviewPromptBuilder, ReviewPromptBuilder>();
        return services;
    }
}
