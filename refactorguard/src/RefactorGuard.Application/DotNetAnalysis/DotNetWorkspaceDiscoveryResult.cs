namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record DotNetWorkspaceDiscoveryResult(
    IReadOnlyList<DotNetWorkspaceCandidate> Candidates,
    DotNetWorkspaceCandidate? Selected,
    IReadOnlyList<string> Warnings);
