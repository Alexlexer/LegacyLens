namespace RefactorGuard.Domain.Git;

public sealed record GitDiffPreviewResponse(
    string RepoPath,
    int ChangedFileCount,
    IReadOnlyList<GitDiffFile> Files,
    string Diff);
