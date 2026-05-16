using System.ComponentModel.DataAnnotations;

namespace LegacyLens.Infrastructure.GpuSearch;

public sealed class GpuSearchOptions
{
    public const string SectionName = "RefactorGuard:GpuSearch";

    [Required]
    public Uri BaseUrl { get; init; } = new("http://127.0.0.1:8765");

    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = 10;
}
