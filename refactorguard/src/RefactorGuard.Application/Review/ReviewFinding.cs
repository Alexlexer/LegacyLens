namespace RefactorGuard.Application.Review;

public sealed record ReviewFinding(
    string RuleId,
    string Severity,
    string? Path,
    string Title,
    string Description);
