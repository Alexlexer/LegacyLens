using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using LegacyLens.Application.Review;
using LegacyLens.Infrastructure.Llm;

namespace LegacyLens.Infrastructure.Tests.Llm;

public sealed class OllamaReviewLlmProviderTests
{
    [Fact]
    public async Task GenerateReviewAsync_SendsChatRequestAndReturnsAssistantContent()
    {
        string? requestBody = null;
        var provider = new OllamaReviewLlmProvider(
            CreateClient(
                new { message = new { role = "assistant", content = "Ollama summary" }, done = true },
                inspectRequest: body => requestBody = body),
            Options.Create(new OllamaOptions { Model = "qwen2.5-coder:7b" }));

        var result = await provider.GenerateReviewAsync(
            new LlmReviewPrompt("repo", [], "diff"),
            CancellationToken.None);

        Assert.Equal("Ollama summary", result);
        using var document = JsonDocument.Parse(requestBody!);
        var root = document.RootElement;
        Assert.Equal("qwen2.5-coder:7b", root.GetProperty("model").GetString());
        Assert.False(root.GetProperty("stream").GetBoolean());

        var messages = root.GetProperty("messages").EnumerateArray().ToArray();
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Contains("Git diff", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task GenerateReviewAsync_IncludesDependencyImpactReasonsInPrompt()
    {
        string? requestBody = null;
        var provider = new OllamaReviewLlmProvider(
            CreateClient(
                new { message = new { role = "assistant", content = "Ollama summary" }, done = true },
                inspectRequest: body => requestBody = body),
            Options.Create(new OllamaOptions { Model = "qwen2.5-coder:7b" }));
        var context = new GpuSearchReviewContext(
            true,
            [
                new ChangedFileContext(
                    "src/UserService.cs",
                    new DependencyImpactSummary(
                        1,
                        ["src/UserController.cs"],
                        ImpactedFiles:
                        [
                            new DependencyImpactedFile(
                                "src/UserController.cs",
                                1,
                                "references type UserService")
                        ]),
                    null,
                    [])
            ]);

        await provider.GenerateReviewAsync(
            new LlmReviewPrompt("repo", [], "diff", context),
            CancellationToken.None);

        Assert.Contains("references type UserService (heuristic)", requestBody ?? string.Empty);
    }

    [Fact]
    public async Task GenerateReviewAsync_ThrowsForEmptyAssistantContent()
    {
        var provider = new OllamaReviewLlmProvider(
            CreateClient(new { message = new { role = "assistant", content = "" }, done = true }),
            Options.Create(new OllamaOptions { Model = "qwen2.5-coder:7b" }));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GenerateReviewAsync(new LlmReviewPrompt("repo", [], "diff"), CancellationToken.None));

        Assert.Contains("empty assistant content", ex.Message);
    }

    [Fact]
    public async Task GenerateReviewAsync_ThrowsForFailureStatus()
    {
        var provider = new OllamaReviewLlmProvider(
            CreateClient(new { error = "failed" }, HttpStatusCode.InternalServerError),
            Options.Create(new OllamaOptions { Model = "qwen2.5-coder:7b" }));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.GenerateReviewAsync(new LlmReviewPrompt("repo", [], "diff"), CancellationToken.None));

        Assert.Contains("Ollama returned 500", ex.Message);
    }

    private static HttpClient CreateClient(
        object response,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        Action<string>? inspectRequest = null)
    {
        return new HttpClient(new StubHandler(response, statusCode, inspectRequest))
        {
            BaseAddress = new Uri("http://127.0.0.1:11434")
        };
    }

    private sealed class StubHandler(
        object response,
        HttpStatusCode statusCode,
        Action<string>? inspectRequest) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/chat", request.RequestUri?.AbsolutePath);
            inspectRequest?.Invoke(await request.Content!.ReadAsStringAsync(cancellationToken));
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json)
            };
        }
    }
}
