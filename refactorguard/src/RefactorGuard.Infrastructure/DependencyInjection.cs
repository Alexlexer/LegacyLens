using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RefactorGuard.Application.Git;
using RefactorGuard.Infrastructure.Git;
using RefactorGuard.Infrastructure.Security;

namespace RefactorGuard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRefactorGuardInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddSingleton<IRepoPathValidator>(new RepoPathValidator(
            configuration.GetSection("RefactorGuard:AllowedRoots").Get<string[]>() ?? []));
        services.AddScoped<IGitDiffService, GitDiffService>();
        return services;
    }
}
