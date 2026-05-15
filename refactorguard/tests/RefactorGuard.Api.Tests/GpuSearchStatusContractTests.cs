using RefactorGuard.Domain.Search;

namespace RefactorGuard.Api.Tests;

public sealed class GpuSearchStatusContractTests
{
    [Fact]
    public void GpuSearchStatusResponse_ExposesStableContract()
    {
        var response = new GpuSearchStatusResponse(
            true,
            new GpuSearchHealth("ok"),
            new GpuSearchStats("ok", "cuda", "RTX 4060", 10),
            null);

        Assert.True(response.IsAvailable);
        Assert.Equal("ok", response.Health?.Status);
        Assert.Equal("cuda", response.Stats?.Backend);
        Assert.Equal(10, response.Stats?.IndexedFileCount);
        Assert.Null(response.Error);
    }
}
