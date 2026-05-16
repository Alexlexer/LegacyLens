using System.ComponentModel.DataAnnotations;

namespace LegacyLens.Infrastructure.DotNetAnalysis;

public sealed class RoslynOptions
{
    public const string SectionName = "RefactorGuard:Roslyn";

    public bool EnableWorkspaceCache { get; init; } = true;

    [Range(1, 20)]
    public int MaxCachedWorkspaces { get; init; } = 3;

    [Range(1, 240)]
    public int CacheTtlMinutes { get; init; } = 30;
}
