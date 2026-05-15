using Microsoft.Extensions.DependencyInjection;
using RefactorGuard.Application;

namespace RefactorGuard.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddRefactorGuardApplication_ReturnsServices()
    {
        var services = new ServiceCollection();

        var result = services.AddRefactorGuardApplication();

        Assert.Same(services, result);
    }
}
