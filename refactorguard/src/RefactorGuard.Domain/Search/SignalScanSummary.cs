namespace RefactorGuard.Domain.Search;

public sealed record SignalScanSummary(
    int SignalCount,
    int MatchCount,
    IReadOnlyList<string> Categories);
