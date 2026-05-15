using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RefactorGuard.Infrastructure;

namespace RefactorGuard.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddRefactorGuardInfrastructure_ReturnsServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var result = services.AddRefactorGuardInfrastructure(configuration);

        Assert.Same(services, result);
    }
}
