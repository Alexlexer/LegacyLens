using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using LegacyLens.Infrastructure.Llm;

namespace LegacyLens.Infrastructure.Tests.Llm;

public sealed class OllamaModelServiceTests
{
    [Fact]
    public async Task GetStatusAsync_ReturnsInstalledWhenTagsContainConfiguredModel()
    {
        var service = CreateService(new QueueResponse(HttpStatusCode.OK, new { models = new[] { new { name = "gemma3:4b" } } }));

        var status = await service.GetStatusAsync(CancellationToken.None);

        Assert.True(status.ServerReachable);
        Assert.True(status.ModelInstalled);
        Assert.Contains("gemma3:4b", status.InstalledModels);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsMissingWhenConfiguredModelAbsent()
    {
        var service = CreateService(new QueueResponse(HttpStatusCode.OK, new { models = new[] { new { name = "phi4-mini" } } }));

        var status = await service.GetStatusAsync(CancellationToken.None);

        Assert.True(status.ServerReachable);
        Assert.False(status.ModelInstalled);
        Assert.Contains("ollama pull gemma3:4b", status.Message);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsUnreachableWhenServerFails()
    {
        var service = CreateService(new QueueResponse(new HttpRequestException("connection refused")));

        var status = await service.GetStatusAsync(CancellationToken.None);

        Assert.False(status.ServerReachable);
        Assert.False(status.ModelInstalled);
        Assert.Contains("not reachable", status.Message);
    }

    [Fact]
    public async Task PullConfiguredModelAsync_PostsPullRequest()
    {
        string? body = null;
        var service = CreateService(new QueueResponse(HttpStatusCode.OK, new { status = "success" }, requestBody => body = requestBody));

        var result = await service.PullConfiguredModelAsync(CancellationToken.None);

        Assert.True(result.Success);
        using var doc = JsonDocument.Parse(body!);
        Assert.Equal("gemma3:4b", doc.RootElement.GetProperty("name").GetString());
        Assert.False(doc.RootElement.GetProperty("stream").GetBoolean());
    }

    [Fact]
    public async Task PullModelAsync_ReturnsControlledErrorOnFailure()
    {
        var service = CreateService(new QueueResponse(HttpStatusCode.InternalServerError, new { error = "failed" }));

        var result = await service.PullModelAsync("gemma3:4b", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("failed to pull", result.Message);
    }

    [Fact]
    public async Task PullModelAsync_RejectsInvalidModelName()
    {
        var service = CreateService(new QueueResponse(HttpStatusCode.OK, new { }));

        var result = await service.PullModelAsync("gemma3:4b; rm -rf /", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid", result.Message);
    }

    private static OllamaModelService CreateService(params QueueResponse[] responses)
    {
        return new OllamaModelService(
            new HttpClient(new QueueHandler(new Queue<QueueResponse>(responses)))
            {
                BaseAddress = new Uri("http://127.0.0.1:11434")
            },
            Options.Create(new OllamaOptions
            {
                Model = "gemma3:4b",
                PullTimeoutSeconds = 600
            }));
    }

    private sealed record QueueResponse(
        HttpStatusCode StatusCode,
        object Response,
        Action<string>? InspectRequest = null)
    {
        public QueueResponse(Exception exception)
            : this(HttpStatusCode.OK, new { }) => Exception = exception;

        public Exception? Exception { get; }
    }

    private sealed class QueueHandler(Queue<QueueResponse> responses) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var next = responses.Dequeue();
            if (next.Exception is not null)
                throw next.Exception;

            if (next.InspectRequest is not null && request.Content is not null)
                next.InspectRequest(await request.Content.ReadAsStringAsync(cancellationToken));

            var json = JsonSerializer.Serialize(next.Response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return new HttpResponseMessage(next.StatusCode)
            {
                Content = new StringContent(json)
            };
        }
    }
}
