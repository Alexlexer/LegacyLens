using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RefactorGuard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRefactorGuardInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return services;
    }
}
