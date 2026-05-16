using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Application.Audit;

public sealed record AuditDependencyInjectionSummary(
    int RegistrationCount,
    int ConstructorDependencyCount,
    int FindingCount,
    IReadOnlyDictionary<string, int> RegistrationsByLifetime,
    IReadOnlyList<DependencyInjectionFinding> Findings);
