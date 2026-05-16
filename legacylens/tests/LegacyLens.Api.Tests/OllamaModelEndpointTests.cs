using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using LegacyLens.Infrastructure.Llm;

namespace LegacyLens.Api.Tests;

public sealed class OllamaModelEndpointTests
{
    [Fact]
    public async Task Status_ReturnsOllamaModelStatus()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var status = await client.GetFromJsonAsync<OllamaModelStatus>("/api/llm/ollama/status");

        Assert.NotNull(status);
        Assert.True(status.ServerReachable);
        Assert.Equal("gemma3:4b", status.ConfiguredModel);
    }

    [Fact]
    public async Task Pull_PullsConfiguredModel()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/llm/ollama/pull", new { });
        var result = await response.Content.ReadFromJsonAsync<OllamaPullResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("gemma3:4b", result.Model);
    }

    [Fact]
    public async Task Pull_RejectsInvalidModelName()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/llm/ollama/pull", new { model = "gemma3;rm" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IOllamaModelService>();
                    services.AddSingleton<IOllamaModelService, StubOllamaModelService>();
                });
            });
    }

    private sealed class StubOllamaModelService : IOllamaModelService
    {
        public Task<OllamaModelStatus> GetStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(new OllamaModelStatus(
                true,
                "http://127.0.0.1:11434",
                "gemma3:4b",
                true,
                ["gemma3:4b"],
                "installed"));

        public Task<OllamaPullResult> PullConfiguredModelAsync(CancellationToken cancellationToken)
            => Task.FromResult(new OllamaPullResult(true, "gemma3:4b", "pulled"));

        public Task<OllamaPullResult> PullModelAsync(string model, CancellationToken cancellationToken)
            => Task.FromResult(new OllamaPullResult(true, model, "pulled"));
    }
}
