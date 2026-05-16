namespace LegacyLens.Infrastructure.Llm;

public interface IOllamaModelService
{
    Task<OllamaModelStatus> GetStatusAsync(CancellationToken cancellationToken);

    Task<OllamaPullResult> PullConfiguredModelAsync(CancellationToken cancellationToken);

    Task<OllamaPullResult> PullModelAsync(string model, CancellationToken cancellationToken);
}
