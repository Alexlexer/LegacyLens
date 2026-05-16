namespace LegacyLens.Domain.Common;

public sealed record ProblemDetailsResponse(
    string Type,
    string Title,
    int Status,
    string Detail);
