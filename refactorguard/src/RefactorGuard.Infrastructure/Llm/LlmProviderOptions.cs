namespace RefactorGuard.Infrastructure.Llm;

public sealed class LlmProviderOptions
{
    public const string SectionName = "RefactorGuard:Review";

    public string Provider { get; init; } = "Deterministic";
}
