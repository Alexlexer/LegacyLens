namespace LegacyLens.Domain.Search;

public sealed record SignalScanSummary(
    int SignalCount,
    int MatchCount,
    IReadOnlyDictionary<string, int> Categories);
