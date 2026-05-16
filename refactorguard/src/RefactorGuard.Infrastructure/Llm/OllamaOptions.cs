using System.ComponentModel.DataAnnotations;

namespace RefactorGuard.Infrastructure.Llm;

public sealed class OllamaOptions
{
    public const string SectionName = "RefactorGuard:Ollama";

    [Required]
    public Uri BaseUrl { get; init; } = new("http://127.0.0.1:11434");

    [Required]
    public string Model { get; init; } = "qwen2.5-coder:7b";

    [Range(1, 600)]
    public int TimeoutSeconds { get; init; } = 120;
}
