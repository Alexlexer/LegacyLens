namespace RefactorGuard.Domain.Search;

public sealed record CodeSearchRequest(
    string Query,
    int TopK = 10,
    bool IncludeContent = false);
