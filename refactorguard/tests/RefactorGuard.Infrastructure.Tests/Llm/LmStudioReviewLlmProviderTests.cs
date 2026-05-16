using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RefactorGuard.Application.Review;
using RefactorGuard.Infrastructure.Llm;

namespace RefactorGuard.Infrastructure.Tests.Llm;

public sealed class LmStudioReviewLlmProviderTests
{
    [Fact]
    public async Task GenerateReviewAsync_ReturnsAssistantContent()
    {
        var provider = new LmStudioReviewLlmProvider(
            CreateClient(new
            {
                choices = new[]
                {
                    new { message = new { role = "assistant", content = "Review summary" } }
                }
            }),
            Options.Create(new LmStudioOptions { Model = "local-model" }));

        var result = await provider.GenerateReviewAsync(
            new LlmReviewPrompt("repo", [], "diff"),
            CancellationToken.None);

        Assert.Equal("Review summary", result);
    }

    [Fact]
    public async Task GenerateReviewAsync_ThrowsForFailureStatus()
    {
        var provider = new LmStudioReviewLlmProvider(
            CreateClient(new { error = "failed" }, HttpStatusCode.InternalServerError),
            Options.Create(new LmStudioOptions { Model = "local-model" }));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.GenerateReviewAsync(new LlmReviewPrompt("repo", [], "diff"), CancellationToken.None));
    }

    [Fact]
    public async Task GenerateReviewAsync_IncludesDependencyImpactReasonsInPrompt()
    {
        string? requestBody = null;
        var provider = new LmStudioReviewLlmProvider(
            CreateClient(
                new
                {
                    choices = new[]
                    {
                        new { message = new { role = "assistant", content = "Review summary" } }
                    }
                },
                inspectRequest: body => requestBody = body),
            Options.Create(new LmStudioOptions { Model = "local-model" }));
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

    private static HttpClient CreateClient(
        object response,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        Action<string>? inspectRequest = null)
    {
        return new HttpClient(new StubHandler(response, statusCode, inspectRequest))
        {
            BaseAddress = new Uri("http://127.0.0.1:1234/v1/")
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
            Assert.Equal("/v1/chat/completions", request.RequestUri?.AbsolutePath);
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
