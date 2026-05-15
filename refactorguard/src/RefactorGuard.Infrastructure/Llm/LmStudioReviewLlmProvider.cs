using System.Net.Http.Json;
using System.Text;
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

        var gpuContext = BuildGpuContextSection(prompt.GpuSearchContext);

        return $"""
            Repository: {prompt.RepoPath}

            Deterministic findings:
            {findings}
            {gpuContext}
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

    private static string BuildGpuContextSection(GpuSearchReviewContext? context)
    {
        if (context is null || !context.WasAvailable || context.Files.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("gpu-search context (factual, retrieved from the indexed repository):");

        foreach (var file in context.Files)
        {
            sb.AppendLine($"  File: {file.FilePath}");

            if (file.DependencyImpact is not null)
                sb.AppendLine($"    Dependency impact: {file.DependencyImpact.TotalImpacted} impacted file(s)");

            if (file.RelatedResults.Count > 0)
            {
                sb.AppendLine("    Related code:");
                foreach (var r in file.RelatedResults.Take(3))
                {
                    var line = r.LineStart.HasValue ? $" L{r.LineStart}" : string.Empty;
                    sb.AppendLine($"      - {r.File}{line}");
                }
            }
        }

        return sb.ToString();
    }

    private sealed record ChatCompletionRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        double Temperature);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatCompletionResponse(IReadOnlyList<ChatChoice> Choices);

    private sealed record ChatChoice(ChatMessage Message);
}
