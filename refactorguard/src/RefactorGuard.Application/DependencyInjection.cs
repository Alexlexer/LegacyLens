using Microsoft.Extensions.DependencyInjection;

namespace RefactorGuard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRefactorGuardApplication(this IServiceCollection services)
    {
        return services;
    }
}
