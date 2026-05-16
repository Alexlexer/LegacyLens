namespace LegacyLens.Application.Review;

public sealed record DiffReviewRequest(string RepoPath, bool UseLlm = false);
