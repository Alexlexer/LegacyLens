namespace RefactorGuard.Domain.Search;

public sealed record GpuSearchStats(
    string Status,
    string? Backend,
    string? Device,
    int? IndexedFileCount);
