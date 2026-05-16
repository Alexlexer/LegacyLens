namespace RefactorGuard.Application.DotNetAnalysis;

public sealed record DependencyInjectionAnalysisResult(
    bool Success,
    string? WorkspacePath,
    DotNetWorkspaceKind? WorkspaceKind,
    IReadOnlyList<ServiceRegistrationInfo> Registrations,
    IReadOnlyList<ConstructorDependencyInfo> ConstructorDependencies,
    IReadOnlyList<DependencyInjectionFinding> Findings,
    IReadOnlyList<string> Warnings,
    string? ErrorMessage);
