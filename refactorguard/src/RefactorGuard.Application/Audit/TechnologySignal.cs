namespace RefactorGuard.Application.Audit;

public sealed record TechnologySignal(
    string Name,
    string Category,
    string Evidence,
    string? FilePath,
    string Confidence);
