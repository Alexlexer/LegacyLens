namespace LegacyLens.Domain.Search;

public sealed record GpuSearchIndexRootRequest(
    string Directory,
    bool RebuildCache = false,
    bool IncludeSemantic = false);
