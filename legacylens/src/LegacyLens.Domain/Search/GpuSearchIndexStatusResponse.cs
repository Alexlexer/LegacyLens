using System.Text.Json;

namespace LegacyLens.Domain.Search;

public sealed class GpuSearchIndexStatusResponse
{
    public IReadOnlyList<JsonElement> IndexedRoots { get; init; } = [];

    public GpuSearchIndexComponentStatus? Pattern { get; init; }

    public GpuSearchIndexComponentStatus? Dependency { get; init; }

    public GpuSearchIndexComponentStatus? Semantic { get; init; }

    public string? Status { get; init; }

    public GpuSearchIndexRootResponse? LastIndexResult { get; init; }

    public string? LastError { get; init; }
}
