using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using LegacyLens.Application.Audit;
using LegacyLens.Application.DotNetAnalysis;
using LegacyLens.Application.Git;
using LegacyLens.Application.Reports;
using LegacyLens.Application.Review;
using LegacyLens.Application.Search;

namespace LegacyLens.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddLegacyLensApplication(this IServiceCollection services)
    {
        services.AddScoped<GitDiffPreviewWorkflow>();
        services.AddScoped<GpuSearchStatusWorkflow>();
        services.AddSingleton<IDotNetAnalysisPresetCatalog, DotNetAnalysisPresetCatalog>();
        services.AddScoped<DotNetAnalysisService>();
        services.AddScoped<IReviewOrchestrator, DiffReviewOrchestrator>();
        services.AddSingleton<IReviewReportFormatter, MarkdownReviewReportFormatter>();
        services.AddSingleton<IReviewPromptBuilder, ReviewPromptBuilder>();
        services.AddScoped<IAuditProvider, TechnologySignalAuditProvider>();
        services.AddScoped<IAuditProvider, RoslynAuditProvider>();
        services.AddScoped<IAuditProvider, DependencyInjectionAuditProvider>();
        services.AddScoped<IAuditProvider, GpuSearchSignalAuditProvider>();
        services.AddScoped<IAuditProvider, ArchitectureSignalAuditProvider>();
        services.AddScoped<IAuditProvider, RecommendedNextStepsAuditProvider>();
        services.AddScoped<ILegacyAuditOrchestrator, LegacyAuditOrchestrator>();
        services.AddSingleton<ILegacyAuditMarkdownFormatter, LegacyAuditMarkdownFormatter>();
        services.AddSingleton<IAuditReportExportService, AuditReportExportService>();
        services.TryAddScoped<IReportRepository, NullReportRepository>();
        return services;
    }
}
