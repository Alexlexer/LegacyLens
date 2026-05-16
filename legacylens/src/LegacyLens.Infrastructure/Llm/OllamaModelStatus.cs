namespace LegacyLens.Infrastructure.Llm;

public sealed record OllamaModelStatus(
    bool ServerReachable,
    string BaseUrl,
    string ConfiguredModel,
    bool ModelInstalled,
    IReadOnlyList<string> InstalledModels,
    string Message,
    string? Error = null);
