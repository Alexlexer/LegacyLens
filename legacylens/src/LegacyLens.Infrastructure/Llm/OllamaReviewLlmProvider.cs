using System.Net.Http.Json;
using System.Text;
using LegacyLens.Application.Review;

namespace LegacyLens.Infrastructure.Llm;

public sealed class OllamaReviewLlmProvider(
    HttpClient httpClient,
    Microsoft.Extensions.Options.IOptions<OllamaOptions> options) : IReviewLlmProvider
{
    public string Name => "Ollama";

    public async Task<string?> GenerateReviewAsync(
        LlmReviewPrompt prompt,
        CancellationToken cancellationToken)
    {
        var request = new OllamaChatRequest(
            options.Value.Model,
            [
                new OllamaChatMessage("system", "You are LegacyLens. Provide concise code review guidance. Do not invent files or claim tests were run."),
                new OllamaChatMessage("user", BuildUserPrompt(prompt))
            ],
            Stream: false);

        using var response = await httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Ollama returned {(int)response.StatusCode}.");
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
        var content = result?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Ollama returned empty assistant content.");
        }

        return content;
    }

    private static string BuildUserPrompt(LlmReviewPrompt prompt)
    {
        var findings = prompt.Findings.Count == 0
            ? "No deterministic findings."
            : string.Join("\n", prompt.Findings.Select(finding =>
                $"- {finding.Severity} {finding.RuleId} {finding.Path}: {finding.Title} - {finding.Description}"));

        var gpuContext = BuildGpuContextSection(prompt.GpuSearchContext);
        var roslynContext = BuildRoslynContextSection(prompt.RoslynContext);

        return $"""
            Repository: {prompt.RepoPath}

            Deterministic findings:
            {findings}
            {gpuContext}{roslynContext}
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
        sb.AppendLine("gpu-search context (advisory — retrieved via heuristic index, not compiler-accurate):");
        sb.AppendLine("IMPORTANT: Dependency impact data is provided by gpu-search-mcp using import/type/name heuristics.");
        sb.AppendLine("Do not treat impacted file counts or importer lists as compiler-verified proof. Treat them as advisory signals.");

        foreach (var file in context.Files)
        {
            sb.AppendLine($"  File: {file.FilePath}");

            if (file.DependencyImpact is not null)
            {
                var di = file.DependencyImpact;
                sb.AppendLine($"    Dependency impact: {di.TotalImpacted} impacted file(s)");

                if (di.Confidence is not null)
                    sb.AppendLine($"    Confidence: {di.Confidence}");

                if (di.AnalysisMode is not null)
                    sb.AppendLine($"    Analysis mode: {di.AnalysisMode} (advisory, not Roslyn-accurate)");

                var warnings = di.Warnings ?? [];
                foreach (var warning in warnings)
                    sb.AppendLine($"    Warning: {warning}");

                var limitations = di.Limitations ?? [];
                foreach (var limitation in limitations)
                    sb.AppendLine($"    Limitation: {limitation}");

                var impactedFiles = di.ImpactedFiles ?? [];
                foreach (var impacted in impactedFiles.Take(5))
                {
                    var reason = string.IsNullOrWhiteSpace(impacted.Reason)
                        ? "reason unavailable"
                        : $"{impacted.Reason} (heuristic)";
                    sb.AppendLine($"    Impacted: {impacted.File}: {reason}");
                }
            }

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

    private static string BuildRoslynContextSection(RoslynReviewContext? context)
    {
        if (context is null || !context.Success || context.ChangedSymbols.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("Roslyn reference context (compiler-accurate C# symbol references):");
        sb.AppendLine("NOTE: These are compiler-verified references. Treat them as stronger evidence than heuristic gpu-search dependency impact.");

        foreach (var symbol in context.ChangedSymbols)
        {
            var refs = context.SymbolReferences
                .Where(r => string.Equals(r.SymbolName, symbol.Name, StringComparison.OrdinalIgnoreCase))
                .Where(r => !r.IsDefinition)
                .ToList();

            sb.AppendLine($"  Changed symbol: {symbol.Name} ({symbol.Kind}) in {symbol.ProjectName}");
            sb.AppendLine($"    Definition: {symbol.FilePath}:{symbol.Line}");
            sb.AppendLine($"    References: {refs.Count}");

            foreach (var r in refs.Take(5))
            {
                var container = string.IsNullOrWhiteSpace(r.ContainingSymbol) ? string.Empty : $" in {r.ContainingSymbol}";
                sb.AppendLine($"      - {r.FilePath}:{r.Line}{container}");
            }
        }

        return sb.ToString();
    }

    private sealed record OllamaChatRequest(
        string Model,
        IReadOnlyList<OllamaChatMessage> Messages,
        bool Stream);

    private sealed record OllamaChatMessage(string Role, string Content);

    private sealed record OllamaChatResponse(OllamaChatMessage? Message);
}
