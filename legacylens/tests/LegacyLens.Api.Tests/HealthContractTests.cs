using LegacyLens.Domain.Common;

namespace LegacyLens.Api.Tests;

public sealed class HealthContractTests
{
    [Fact]
    public void Healthy_ReturnsStableHealthContract()
    {
        var health = SystemHealth.Healthy();

        Assert.Equal("ok", health.Status);
        Assert.Equal("LegacyLens", health.Service);
    }
}
