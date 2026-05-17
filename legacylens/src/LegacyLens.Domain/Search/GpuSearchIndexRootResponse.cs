namespace LegacyLens.Domain.Search;

public sealed class GpuSearchIndexRootResponse
{
    public bool Ok { get; init; }

    public string? Directory { get; init; }

    public string? NormalizedDirectory { get; init; }

    public bool? Started { get; init; }

    public bool? Completed { get; init; }

    public GpuSearchIndexComponentStatus? Pattern { get; init; }

    public GpuSearchIndexComponentStatus? Dependency { get; init; }

    public GpuSearchIndexComponentStatus? Semantic { get; init; }

    public string? Message { get; init; }

    public string? Error { get; init; }
}
