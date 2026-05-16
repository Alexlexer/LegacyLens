namespace LegacyLens.Application.Audit;

public sealed class RecommendedNextStepsAuditProvider : IAuditProvider
{
    public string Name => "RecommendedNextSteps";

    public Task<AuditProviderResult> AnalyzeAsync(
        AuditContext context,
        CancellationToken cancellationToken)
    {
        var steps = BuildRecommendedNextSteps(
            context.RiskFindings,
            context.RoslynSummary,
            context.DependencyInjectionSummary);
        return Task.FromResult(new AuditProviderResult(Name, RecommendedNextSteps: steps));
    }

    private static IReadOnlyList<string> BuildRecommendedNextSteps(
        IReadOnlyList<AuditFinding> findings,
        AuditRoslynSummary? roslyn,
        AuditDependencyInjectionSummary? di)
    {
        var steps = new List<string>();
        var findingCodes = new HashSet<string>(findings.Select(f => f.Code), StringComparer.OrdinalIgnoreCase);

        if (findingCodes.Contains("legacy-framework-detected") || findingCodes.Contains("web-config-present"))
            steps.Add("Plan migration from .NET Framework to .NET 8/9. Use the .NET Upgrade Assistant for initial guidance.");

        if (findingCodes.Contains("packages-config-present"))
            steps.Add("Migrate from packages.config to PackageReference in .csproj to unlock transitive dependency pruning.");

        if (findingCodes.Contains("no-tests-detected"))
            steps.Add("Add unit and integration tests before starting modernization. Tests are a safety net for refactoring.");

        if (findingCodes.Contains("broad-exception-catch"))
            steps.Add("Replace broad catch(Exception) handlers with specific exception types or structured error handling.");

        if (findingCodes.Contains("sync-over-async"))
            steps.Add("Replace .Result and .Wait() blocking calls with async/await throughout the call chain.");

        if (findingCodes.Contains("raw-sql-usage"))
            steps.Add("Review direct SQL usage for injection risks. Consider migrating to parameterized queries or a safe ORM layer.");

        if (findingCodes.Contains("service-locator-usage"))
            steps.Add("Replace service locator calls with constructor injection. Service locator hides dependencies and complicates testing.");

        if (di is { FindingCount: > 0 })
            steps.Add("Review DI analysis findings. Address singleton-depends-on-scoped and missing registration candidates first.");

        if (roslyn is { WorkspaceLoaded: false })
            steps.Add("Resolve Roslyn workspace load failures to unlock deeper compiler-aware analysis.");

        if (steps.Count == 0)
        {
            steps.Add("Review technology signals and architecture signals above for specific action areas.");
            steps.Add("Run with IncludeRoslyn=true and IncludeGpuSearch=true for the most complete audit.");
        }

        return steps;
    }
}
