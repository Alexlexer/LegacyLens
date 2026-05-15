namespace RefactorGuard.Domain.Git;

public sealed record GitDiffFile(
    string Path,
    string Status,
    int Additions,
    int Deletions);
