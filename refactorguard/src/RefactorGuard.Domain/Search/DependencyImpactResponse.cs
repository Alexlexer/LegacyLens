namespace RefactorGuard.Domain.Search;

public sealed record DependencyImpactResponse(
    string Result,
    string File,
    string? AbsoluteFile,
    IReadOnlyList<ImpactedFile> ImpactedFiles);
