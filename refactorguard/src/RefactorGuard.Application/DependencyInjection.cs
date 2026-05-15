using Microsoft.Extensions.DependencyInjection;
using RefactorGuard.Application.Git;
using RefactorGuard.Application.Search;

namespace RefactorGuard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRefactorGuardApplication(this IServiceCollection services)
    {
        services.AddScoped<GitDiffPreviewWorkflow>();
        services.AddScoped<GpuSearchStatusWorkflow>();
        return services;
    }
}
