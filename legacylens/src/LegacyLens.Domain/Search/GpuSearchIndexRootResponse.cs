namespace LegacyLens.Domain.Search;

public sealed record GpuSearchIndexRootResponse(
    bool Ok,
    string? Directory,
    string? NormalizedDirectory,
    bool Started,
    bool Completed,
    GpuSearchIndexPatternResult? Pattern,
    GpuSearchIndexDepResult? Dependency,
    GpuSearchIndexSemanticResult? Semantic,
    string? Message);

public sealed record GpuSearchIndexPatternResult(
    bool Ready,
    int Files,
    bool FromCache);

public sealed record GpuSearchIndexDepResult(
    bool Ready,
    int Files);

public sealed record GpuSearchIndexSemanticResult(
    bool Requested,
    bool Ready,
    string? Message);
