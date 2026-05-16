namespace RefactorGuard.Domain.Search;

public sealed record SignalScanResponse(
    string Result,
    IReadOnlyList<string> Categories,
    SignalScanSummary? Summary,
    IReadOnlyList<RepositorySignal> Signals,
    IReadOnlyList<string>? Limitations,
    IReadOnlyList<string>? Warnings);
