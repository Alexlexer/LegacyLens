using System.ComponentModel.DataAnnotations;

namespace LegacyLens.Infrastructure.Llm;

public sealed class LmStudioOptions
{
    public const string SectionName = "RefactorGuard:LmStudio";

    [Required]
    public Uri BaseUrl { get; init; } = new("http://127.0.0.1:1234/v1/");

    [Required]
    public string Model { get; init; } = "local-model";

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 60;
}
