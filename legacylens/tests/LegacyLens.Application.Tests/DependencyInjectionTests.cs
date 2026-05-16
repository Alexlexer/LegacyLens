using Microsoft.Extensions.DependencyInjection;
using LegacyLens.Application;

namespace LegacyLens.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddLegacyLensApplication_ReturnsServices()
    {
        var services = new ServiceCollection();

        var result = services.AddLegacyLensApplication();

        Assert.Same(services, result);
    }
}
