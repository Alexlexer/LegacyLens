using System.Net.Http.Json;
using RefactorGuard.Application.Review;

namespace RefactorGuard.Infrastructure.Llm;

public sealed class LmStudioReviewLlmProvider(
    HttpClient httpClient,
    Microsoft.Extensions.Options.IOptions<LmStudioOptions> options) : IReviewLlmProvider
{
    public string Name => "LmStudio";

    public async Task<string?> GenerateReviewAsync(
        LlmReviewPrompt prompt,
        CancellationToken cancellationToken)
    {
        var request = new ChatCompletionRequest(
            options.Value.Model,
            [
                new ChatMessage("system", "You are RefactorGuard. Provide concise code review guidance. Do not invent files or claim tests were run."),
                new ChatMessage("user", BuildUserPrompt(prompt))
            ],
            0.2);

        using var response = await httpClient.PostAsJsonAsync("chat/completions", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"LM Studio returned {(int)response.StatusCode}.");
        }

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);
        return result?.Choices.FirstOrDefault()?.Message.Content;
    }

    private static string BuildUserPrompt(LlmReviewPrompt prompt)
    {
        var findings = prompt.Findings.Count == 0
            ? "No deterministic findings."
            : string.Join("\n", prompt.Findings.Select(finding =>
                $"- {finding.Severity} {finding.RuleId} {finding.Path}: {finding.Title} - {finding.Description}"));

        return $"""
            Repository: {prompt.RepoPath}

            Deterministic findings:
            {findings}

            Git diff:
            ```diff
            {prompt.Diff}
            ```

            Return:
            1. Top risks.
            2. Suggested review focus.
            3. Missing tests to consider.
            """;
    }

    private sealed record ChatCompletionRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        double Temperature);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatCompletionResponse(IReadOnlyList<ChatChoice> Choices);

    private sealed record ChatChoice(ChatMessage Message);
}
