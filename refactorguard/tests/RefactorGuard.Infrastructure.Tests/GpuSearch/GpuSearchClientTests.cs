using System.Net;
using System.Text.Json;
using RefactorGuard.Domain.Search;
using RefactorGuard.Infrastructure.GpuSearch;

namespace RefactorGuard.Infrastructure.Tests.GpuSearch;

public sealed class GpuSearchClientTests
{
    [Fact]
    public async Task GetHealthAsync_ReturnsHealthResponse()
    {
        var client = CreateClient(new { status = "ok" });
        var gpuSearchClient = new GpuSearchClient(client);

        var health = await gpuSearchClient.GetHealthAsync(CancellationToken.None);

        Assert.Equal("ok", health.Status);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsStatsResponse()
    {
        var client = CreateClient(new
        {
            status = "ok",
            backend = "cuda",
            device = "RTX 4060",
            indexedFileCount = 42
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var stats = await gpuSearchClient.GetStatsAsync(CancellationToken.None);

        Assert.Equal("ok", stats.Status);
        Assert.Equal("cuda", stats.Backend);
        Assert.Equal("RTX 4060", stats.Device);
        Assert.Equal(42, stats.IndexedFileCount);
    }

    [Fact]
    public async Task GetHealthAsync_ThrowsWhenServerReturnsFailure()
    {
        var client = CreateClient(new { error = "failed" }, HttpStatusCode.ServiceUnavailable);
        var gpuSearchClient = new GpuSearchClient(client);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            gpuSearchClient.GetHealthAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SearchHybridAsync_ReturnsWrappedResults()
    {
        var client = CreateClient(new
        {
            results = new[]
            {
                new { filePath = "src/App.cs", line = 10, snippet = "Task.Result", score = 0.9 }
            }
        });
        var gpuSearchClient = new GpuSearchClient(client);

        var results = await gpuSearchClient.SearchHybridAsync(
            new SearchHybridRequest("Task.Result", "repo", 5),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("src/App.cs", results[0].FilePath);
    }

    private static HttpClient CreateClient(object response, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpClient(new StubHandler(response, statusCode))
        {
            BaseAddress = new Uri("http://127.0.0.1:8765")
        };
    }

    private sealed class StubHandler(object response, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json)
            });
        }
    }
}
