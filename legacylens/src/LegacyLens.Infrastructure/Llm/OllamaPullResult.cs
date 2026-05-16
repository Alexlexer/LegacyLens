namespace LegacyLens.Infrastructure.Llm;

public sealed record OllamaPullResult(
    bool Success,
    string Model,
    string Message,
    string? Error = null);
