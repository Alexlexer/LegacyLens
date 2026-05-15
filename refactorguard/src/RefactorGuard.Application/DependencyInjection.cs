using Microsoft.Extensions.DependencyInjection;
using RefactorGuard.Application.Git;

namespace RefactorGuard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRefactorGuardApplication(this IServiceCollection services)
    {
        services.AddScoped<GitDiffPreviewWorkflow>();
        return services;
    }
}
