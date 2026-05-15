namespace RefactorGuard.Application.Review;

public sealed record GpuSearchReviewContext(
    bool WasAvailable,
    IReadOnlyList<ChangedFileContext> Files,
    string? UnavailableReason = null);

public sealed record ChangedFileContext(
    string FilePath,
    DependencyImpactSummary? DependencyImpact,
    SkeletonSummary? Skeleton,
    IReadOnlyList<RelatedCodeResult> RelatedResults,
    string? Error = null);

public sealed record DependencyImpactSummary(
    int TotalImpacted,
    IReadOnlyList<string> DirectImporters,
    string? Confidence = null,
    string? AnalysisMode = null,
    IReadOnlyList<string>? Limitations = null,
    IReadOnlyList<string>? Warnings = null);

public sealed record SkeletonSummary(string Content, string? Language);

public sealed record RelatedCodeResult(
    string File,
    int? LineStart,
    int? LineEnd,
    string? Snippet,
    string? Engine,
    double? Score);
