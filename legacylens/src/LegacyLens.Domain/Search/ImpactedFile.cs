namespace LegacyLens.Domain.Search;

public sealed record ImpactedFile(
    string File,
    string? AbsoluteFile,
    int Hops,
    string? Reason = null);
