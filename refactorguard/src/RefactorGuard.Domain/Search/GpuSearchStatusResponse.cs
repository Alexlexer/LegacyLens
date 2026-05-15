namespace RefactorGuard.Domain.Search;

public sealed record GpuSearchStatusResponse(
    bool IsAvailable,
    GpuSearchHealth? Health,
    GpuSearchStats? Stats,
    string? Error);
