using RefactorGuard.Domain.Common;

namespace RefactorGuard.Api.Tests;

public sealed class HealthContractTests
{
    [Fact]
    public void Healthy_ReturnsStableHealthContract()
    {
        var health = SystemHealth.Healthy();

        Assert.Equal("ok", health.Status);
        Assert.Equal("RefactorGuard", health.Service);
    }
}
