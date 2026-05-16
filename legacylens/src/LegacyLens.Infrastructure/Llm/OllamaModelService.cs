using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace LegacyLens.Infrastructure.Llm;

public sealed partial class OllamaModelService(
    HttpClient httpClient,
    IOptions<OllamaOptions> options) : IOllamaModelService
{
    public async Task<OllamaModelStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var configuredModel = options.Value.Model;
        try
        {
            using var response = await httpClient.GetAsync("api/tags", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new OllamaModelStatus(
                    false,
                    options.Value.BaseUrl.ToString(),
                    configuredModel,
                    false,
                    [],
                    $"Ollama returned {(int)response.StatusCode} from /api/tags.",
                    $"HTTP {(int)response.StatusCode}");
            }

            var tags = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(cancellationToken);
            var models = tags?.Models.Select(m => m.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList() ?? [];
            var installed = models.Any(model => string.Equals(model, configuredModel, StringComparison.OrdinalIgnoreCase));
            var message = installed
                ? $"Ollama model '{configuredModel}' is installed."
                : $"Ollama model '{configuredModel}' is not installed. Pull it from the UI or run: ollama pull {configuredModel}";

            return new OllamaModelStatus(
                true,
                options.Value.BaseUrl.ToString(),
                configuredModel,
                installed,
                models,
                message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return new OllamaModelStatus(
                false,
                options.Value.BaseUrl.ToString(),
                configuredModel,
                false,
                [],
                $"Ollama is not reachable at {options.Value.BaseUrl}. Start Ollama or disable LLM.",
                ex.Message);
        }
    }

    public Task<OllamaPullResult> PullConfiguredModelAsync(CancellationToken cancellationToken)
        => PullModelAsync(options.Value.Model, cancellationToken);

    public async Task<OllamaPullResult> PullModelAsync(string model, CancellationToken cancellationToken)
    {
        if (!IsValidModelName(model))
        {
            return new OllamaPullResult(false, model, "Invalid Ollama model name.", "Model names may contain letters, numbers, dash, underscore, slash, dot, and colon.");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.PullTimeoutSeconds));
            using var response = await httpClient.PostAsJsonAsync(
                "api/pull",
                new OllamaPullRequest(model, Stream: false),
                timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new OllamaPullResult(
                    false,
                    model,
                    $"Ollama failed to pull '{model}'.",
                    string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : body);
            }

            return new OllamaPullResult(true, model, $"Ollama model '{model}' is available.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return new OllamaPullResult(
                false,
                model,
                $"Ollama failed to pull '{model}'.",
                ex.Message);
        }
    }

    public static bool IsValidModelName(string? model)
        => !string.IsNullOrWhiteSpace(model) && ModelNameRegex().IsMatch(model);

    [GeneratedRegex("^[A-Za-z0-9._:/-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex ModelNameRegex();

    private sealed record OllamaTagsResponse(IReadOnlyList<OllamaTagModel> Models);

    private sealed record OllamaTagModel(string Name);

    private sealed record OllamaPullRequest(string Name, bool Stream);
}
