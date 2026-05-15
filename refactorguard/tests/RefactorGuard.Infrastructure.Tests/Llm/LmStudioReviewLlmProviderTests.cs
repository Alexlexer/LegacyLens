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

    private static HttpClient CreateClient(object response, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpClient(new StubHandler(response, statusCode))
        {
            BaseAddress = new Uri("http://127.0.0.1:1234/v1/")
        };
    }

    private sealed class StubHandler(object response, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/v1/chat/completions", request.RequestUri?.AbsolutePath);
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
