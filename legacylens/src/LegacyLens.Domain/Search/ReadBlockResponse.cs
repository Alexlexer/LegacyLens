namespace LegacyLens.Domain.Search;

public sealed record ReadBlockResponse(
    string Result,
    string File,
    string? AbsoluteFile,
    int? LineStart,
    int? LineEnd,
    string? Content,
    string? Language);
