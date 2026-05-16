using RefactorGuard.Application.DotNetAnalysis;

namespace RefactorGuard.Application.Audit;

public sealed record AuditDependencyInjectionSummary(
    int RegistrationCount,
    int ConstructorDependencyCount,
    int FindingCount,
    IReadOnlyDictionary<string, int> RegistrationsByLifetime,
    IReadOnlyList<DependencyInjectionFinding> Findings);
