using RefactorGuard.Domain.Git;

namespace RefactorGuard.Application.Review;

public sealed record DiffReviewReport(
    string ReportId,
    string RepoPath,
    DateTimeOffset GeneratedAtUtc,
    int ChangedFileCount,
    IReadOnlyList<GitDiffFile> Files,
    IReadOnlyList<ReviewFinding> Findings,
    string Markdown,
    string? LlmSummary = null,
    string LlmProvider = "Deterministic");
