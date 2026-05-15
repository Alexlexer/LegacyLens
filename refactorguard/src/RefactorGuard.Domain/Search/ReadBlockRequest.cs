namespace RefactorGuard.Domain.Search;

public sealed record ReadBlockRequest(
    string Path,
    int Line,
    int ContextLines = 20);
